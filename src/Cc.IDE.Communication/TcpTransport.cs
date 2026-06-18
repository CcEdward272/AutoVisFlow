using System.Net.Sockets;

namespace Cc.IDE.Communication;

/// <summary>
/// 基于 <see cref="TcpClient"/> 的 TCP/IP 套接字通信传输。
/// 适用于具有 LXI/VXI-11 或原始 TCP SCPI 接口的仪器。
/// </summary>
/// <remarks>
/// 支持可配置的主机/端口、连接超时、Keep-Alive、Nagle 算法控制以及终止字符处理。
/// 后续阶段将添加 TLS 支持（LXI Secure）。
/// </remarks>
public sealed class TcpTransport : ICommunicationTransport
{
    private TcpClient? _tcpClient;
    private NetworkStream? _networkStream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly object _lock = new();
    private readonly TcpTransportConfig _config;

    /// <summary>
    /// 使用指定的 TCP 配置创建传输实例。
    /// </summary>
    /// <param name="config">TCP 传输配置参数。</param>
    public TcpTransport(TcpTransportConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// 使用默认配置创建传输实例（localhost:5025）。
    /// </summary>
    public TcpTransport() : this(new TcpTransportConfig())
    {
    }

    /// <inheritdoc />
    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _tcpClient is { Connected: true };
            }
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">已处于连接状态时抛出。</exception>
    /// <exception cref="TimeoutException">连接超时时抛出。</exception>
    /// <exception cref="SocketException">网络错误时抛出。</exception>
    public async Task ConnectAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            if (_tcpClient is { Connected: true })
                throw new InvalidOperationException("TCP 传输已处于连接状态。");
        }

        ct.ThrowIfCancellationRequested();

        var client = new TcpClient
        {
            NoDelay = _config.NoDelay
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_config.ConnectTimeoutMs);

        try
        {
            await client.ConnectAsync(_config.Host, _config.Port, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            client.Dispose();
            throw new TimeoutException($"TCP 连接到 {_config.Host}:{_config.Port} 超时（{_config.ConnectTimeoutMs}ms）。");
        }
        catch (SocketException ex)
        {
            client.Dispose();
            throw new InvalidOperationException($"无法连接到 TCP {_config.Host}:{_config.Port}：{ex.Message}", ex);
        }

        if (_config.UseKeepAlive)
        {
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

        var stream = client.GetStream();
        var reader = new StreamReader(stream);
        var writer = new StreamWriter(stream)
        {
            AutoFlush = true,
            NewLine = _config.TerminationChar
        };

        lock (_lock)
        {
            _tcpClient = client;
            _networkStream = stream;
            _reader = reader;
            _writer = writer;
        }
    }

    /// <summary>
    /// 断开 TCP 连接并释放所有网络流、读取器和写入器资源。
    /// </summary>
    public Task DisconnectAsync()
    {
        lock (_lock)
        {
            try
            {
                _writer?.Flush();
            }
            catch (IOException) { }

            _reader?.Dispose();
            _reader = null;
            _writer?.Dispose();
            _writer = null;
            _networkStream?.Dispose();
            _networkStream = null;

            try
            {
                _tcpClient?.Close();
            }
            catch (SocketException) { }

            _tcpClient?.Dispose();
            _tcpClient = null;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">未连接时抛出。</exception>
    /// <exception cref="TimeoutException">写入超时时抛出。</exception>
    public async Task SendAsync(string data, CancellationToken ct)
    {
        StreamWriter writer;
        lock (_lock)
        {
            if (_tcpClient is not { Connected: true } || _writer is null)
                throw new InvalidOperationException("未连接——无法发送数据。");

            writer = _writer;
        }

        ct.ThrowIfCancellationRequested();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_config.WriteTimeoutMs);

        try
        {
            await writer.WriteLineAsync(data.AsMemory(), timeoutCts.Token);
            await writer.FlushAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"向 {_config.Host}:{_config.Port} 写入数据超时（{_config.WriteTimeoutMs}ms）。");
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">未连接时抛出。</exception>
    /// <exception cref="TimeoutException">读取超时时抛出。</exception>
    public async Task<string> ReceiveAsync(CancellationToken ct)
    {
        StreamReader reader;
        lock (_lock)
        {
            if (_tcpClient is not { Connected: true } || _reader is null)
                throw new InvalidOperationException("未连接——无法接收数据。");

            reader = _reader;
        }

        ct.ThrowIfCancellationRequested();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_config.ReadTimeoutMs);

        try
        {
            if (_config.EnableTerminationChar)
            {
                var line = await reader.ReadLineAsync(timeoutCts.Token);
                return line ?? string.Empty;
            }
            else
            {
                // 无终止字符：尝试读取可用数据
                var buffer = new char[4096];
                var readTask = reader.ReadAsync(buffer, 0, buffer.Length);
                var completedTask = await Task.WhenAny(readTask, Task.Delay(_config.ReadTimeoutMs, timeoutCts.Token));

                if (completedTask == readTask)
                {
                    var charsRead = await readTask;
                    return new string(buffer, 0, charsRead);
                }

                throw new TimeoutException($"从 {_config.Host}:{_config.Port} 读取数据超时（{_config.ReadTimeoutMs}ms）。");
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"从 {_config.Host}:{_config.Port} 读取数据超时（{_config.ReadTimeoutMs}ms）。");
        }
    }

    /// <summary>
    /// 发送命令并接收响应。先发送命令，然后读取返回数据。
    /// </summary>
    /// <param name="command">要发送的命令字符串。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>来自设备的响应字符串。</returns>
    public async Task<string> QueryAsync(string command, CancellationToken ct)
    {
        await SendAsync(command, ct);
        return await ReceiveAsync(ct);
    }

    /// <summary>
    /// 释放 TCP 连接占用的所有资源，包括网络流、读取器、写入器和客户端。
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            try { _writer?.Flush(); } catch (IOException) { }

            _reader?.Dispose();
            _reader = null;
            _writer?.Dispose();
            _writer = null;
            _networkStream?.Dispose();
            _networkStream = null;

            try { _tcpClient?.Close(); } catch (SocketException) { }
            _tcpClient?.Dispose();
            _tcpClient = null;
        }

        GC.SuppressFinalize(this);
    }
}
