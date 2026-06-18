using System.IO.Ports;

namespace Cc.IDE.Communication;

/// <summary>
/// 基于 <see cref="SerialPort"/> 的 RS-232/RS-485 串口通信传输。
/// 支持可配置的波特率、数据位、校验位、停止位、终止字符处理和超时错误恢复。
/// </summary>
public sealed class SerialTransport : ICommunicationTransport
{
    private SerialPort? _serialPort;
    private readonly object _lock = new();
    private readonly SerialTransportConfig _config;

    /// <summary>
    /// 使用指定的串口配置创建传输实例。
    /// </summary>
    /// <param name="config">串口传输配置参数。</param>
    public SerialTransport(SerialTransportConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// 使用默认配置创建传输实例（COM1，9600-8-N-1）。
    /// </summary>
    public SerialTransport() : this(new SerialTransportConfig())
    {
    }

    /// <inheritdoc />
    public bool IsConnected
    {
        get
        {
            lock (_lock)
            {
                return _serialPort is { IsOpen: true };
            }
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">已处于连接状态时抛出。</exception>
    /// <exception cref="TimeoutException">端口打开超时时抛出。</exception>
    /// <exception cref="UnauthorizedAccessException">端口被占用或权限不足时抛出。</exception>
    /// <exception cref="IOException">端口通信错误时抛出。</exception>
    public Task ConnectAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            if (_serialPort is { IsOpen: true })
                throw new InvalidOperationException("串口传输已处于连接状态。");

            ct.ThrowIfCancellationRequested();

            _serialPort = new SerialPort(_config.PortName, _config.BaudRate, _config.Parity, _config.DataBits, _config.StopBits)
            {
                ReadTimeout = _config.ReadTimeoutMs,
                WriteTimeout = _config.WriteTimeoutMs,
                Handshake = _config.EnableHandshake ? Handshake.RequestToSend : Handshake.None,
                NewLine = _config.TerminationChar
            };

            try
            {
                _serialPort.Open();
            }
            catch (UnauthorizedAccessException)
            {
                _serialPort.Dispose();
                _serialPort = null;
                throw;
            }
            catch (IOException)
            {
                _serialPort.Dispose();
                _serialPort = null;
                throw;
            }
            catch (TimeoutException)
            {
                _serialPort.Dispose();
                _serialPort = null;
                throw;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 断开与串行端口的连接，丢弃缓冲区数据并释放端口资源。
    /// </summary>
    public Task DisconnectAsync()
    {
        lock (_lock)
        {
            if (_serialPort is null)
                return Task.CompletedTask;

            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                    _serialPort.Close();
                }
            }
            catch (IOException)
            {
                // 关闭时忽略 IO 异常
            }
            finally
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">未连接时抛出。</exception>
    /// <exception cref="TimeoutException">写入超时时抛出。</exception>
    public Task SendAsync(string data, CancellationToken ct)
    {
        lock (_lock)
        {
            if (_serialPort is not { IsOpen: true })
                throw new InvalidOperationException("未连接——无法发送数据。");

            ct.ThrowIfCancellationRequested();

            try
            {
                _serialPort.WriteLine(data);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"向 {_config.PortName} 写入数据超时（{_config.WriteTimeoutMs}ms）。");
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">未连接时抛出。</exception>
    /// <exception cref="TimeoutException">读取超时时抛出。</exception>
    public Task<string> ReceiveAsync(CancellationToken ct)
    {
        SerialPort port;
        lock (_lock)
        {
            if (_serialPort is not { IsOpen: true })
                throw new InvalidOperationException("未连接——无法接收数据。");

            port = _serialPort;
        }

        ct.ThrowIfCancellationRequested();

        try
        {
            if (_config.EnableTerminationChar)
            {
                var result = port.ReadLine();
                return Task.FromResult(result);
            }
            else
            {
                // 无终止字符：读取所有可用字节
                var buffer = new byte[port.BytesToRead > 0 ? port.BytesToRead : 4096];
                var bytesRead = port.Read(buffer, 0, buffer.Length);
                return Task.FromResult(System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead));
            }
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"从 {_config.PortName} 读取数据超时（{_config.ReadTimeoutMs}ms）。");
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
    /// 释放串行端口占用的所有资源，并阻止垃圾回收器调用终结器。
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_serialPort is not null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.DiscardInBuffer();
                        _serialPort.DiscardOutBuffer();
                        _serialPort.Close();
                    }
                }
                catch (IOException)
                {
                    // 释放时忽略 IO 异常
                }

                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        GC.SuppressFinalize(this);
    }
}
