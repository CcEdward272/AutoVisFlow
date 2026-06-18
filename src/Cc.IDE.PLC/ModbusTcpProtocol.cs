using System.Buffers.Binary;
using System.Net.Sockets;
using Cc.IDE.Communication;

namespace Cc.IDE.PLC;

/// <summary>
/// 基于 TCP/IP 的工业级 Modbus TCP 协议实现。
/// </summary>
/// <remarks>
/// 完整实现 Modbus TCP/IP 协议栈，包含以下功能：
/// <list type="bullet">
///   <item><description>Function Code 01：读取线圈（Read Coils）</description></item>
///   <item><description>Function Code 02：读取离散输入（Read Discrete Inputs）</description></item>
///   <item><description>Function Code 03：读取保持寄存器（Read Holding Registers）</description></item>
///   <item><description>Function Code 04：读取输入寄存器（Read Input Registers）</description></item>
///   <item><description>Function Code 05：写入单个线圈（Write Single Coil）</description></item>
///   <item><description>Function Code 06：写入单个寄存器（Write Single Register）</description></item>
///   <item><description>Function Code 15 (0x0F)：写入多个线圈（Write Multiple Coils）</description></item>
///   <item><description>Function Code 16 (0x10)：写入多个寄存器（Write Multiple Registers）</description></item>
/// </list>
/// 使用 <see cref="Cc.IDE.Communication.TcpTransportConfig"/> 进行连接配置，
/// 内部通过原始 <see cref="TcpClient"/> / <see cref="NetworkStream"/> 实现二进制帧级的读写操作。
/// 所有公共方法均为线程安全。
/// </remarks>
public sealed class ModbusTcpProtocol : IPlcProtocol, IDisposable
{
    // ──────────────────────────── 字段 ────────────────────────────

    private TcpClient? _tcpClient;
    private NetworkStream? _networkStream;
    private readonly object _lock = new();
    private int _nextTransactionId;
    private IOConnectionConfig _config = new();
    private bool _disposed;

    // ──────────────────────────── 公共属性 ────────────────────────────

    /// <summary>获取一个值，指示是否已连接到 PLC 设备。</summary>
    public bool IsConnected
    {
        get
        {
            lock (_lock)
                return _tcpClient is { Connected: true };
        }
    }

    // ──────────────────────────── IPlcProtocol ────────────────────────────

    /// <summary>
    /// 连接到 Modbus TCP 设备。根据 <paramref name="config"/> 中指定的
    /// <see cref="IOConnectionConfig.Host"/> 和 <see cref="IOConnectionConfig.Port"/> 创建 TCP 连接。
    /// </summary>
    /// <param name="config">连接配置参数，包含主机地址、端口及从站 ID。</param>
    /// <param name="ct">取消令牌，用于取消连接操作。</param>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> 为 null 时抛出。</exception>
    /// <exception cref="InvalidOperationException">已处于连接状态时抛出。</exception>
    /// <exception cref="TimeoutException">连接超时时抛出。</exception>
    /// <exception cref="SocketException">网络错误时抛出。</exception>
    public async Task ConnectAsync(IOConnectionConfig config, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(config);

        lock (_lock)
        {
            if (_tcpClient is { Connected: true })
                throw new InvalidOperationException("Modbus TCP 已处于连接状态。请先调用 DisconnectAsync。");
        }

        ct.ThrowIfCancellationRequested();

        _config = config;

        var tcpConfig = new TcpTransportConfig
        {
            Host = config.Host,
            Port = config.Port,
            ConnectTimeoutMs = config.TimeoutMs,
            NoDelay = true,
            UseKeepAlive = true,
            EnableTerminationChar = false,
        };

        var client = new TcpClient
        {
            NoDelay = tcpConfig.NoDelay,
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(tcpConfig.ConnectTimeoutMs);

        try
        {
            await client.ConnectAsync(tcpConfig.Host, tcpConfig.Port, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            client.Dispose();
            throw new TimeoutException(
                $"Modbus TCP 连接到 {tcpConfig.Host}:{tcpConfig.Port} 超时（{tcpConfig.ConnectTimeoutMs}ms）。");
        }
        catch (SocketException ex)
        {
            client.Dispose();
            throw new InvalidOperationException(
                $"无法连接到 Modbus TCP {tcpConfig.Host}:{tcpConfig.Port}：{ex.Message}", ex);
        }

        if (tcpConfig.UseKeepAlive)
            client.Client?.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

        var stream = client.GetStream();
        stream.ReadTimeout = config.TimeoutMs;
        stream.WriteTimeout = config.TimeoutMs;

        lock (_lock)
        {
            _tcpClient = client;
            _networkStream = stream;
            _nextTransactionId = 1;
        }
    }

    /// <summary>
    /// 断开与 Modbus TCP 设备的连接，释放 TCP 客户端和网络流资源。
    /// </summary>
    public Task DisconnectAsync()
    {
        lock (_lock)
        {
            _networkStream?.Dispose();
            _networkStream = null;
            try { _tcpClient?.Close(); } catch (SocketException) { }
            _tcpClient?.Dispose();
            _tcpClient = null;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 读取单个线圈（Function Code 01）。
    /// </summary>
    /// <param name="address">线圈地址（0-based）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>线圈的布尔状态值。</returns>
    /// <exception cref="TimeoutException">读取超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task<bool> ReadCoilAsync(int address, CancellationToken ct)
    {
        var request = ModbusTcpFrame.BuildReadCoilsRequest(GetNextTransactionId(), GetUnitId(), address, 1);
        var response = await ExecuteWithRetryAsync(request, ct);
        var data = ParseReadResponse(response, 1);
        return data[0] == 1;
    }

    /// <summary>
    /// 写入单个线圈（Function Code 05）。
    /// </summary>
    /// <param name="address">线圈地址（0-based）。</param>
    /// <param name="value">要写入的布尔值（true = ON / 0xFF00, false = OFF / 0x0000）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="TimeoutException">写入超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task WriteCoilAsync(int address, bool value, CancellationToken ct)
    {
        var request = ModbusTcpFrame.BuildWriteSingleCoilRequest(GetNextTransactionId(), GetUnitId(), address, value);
        await ExecuteWithRetryAsync(request, ct);
    }

    /// <summary>
    /// 读取单个保持寄存器（Function Code 03）。
    /// </summary>
    /// <param name="address">寄存器地址（0-based）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>寄存器的无符号 16 位整数值。</returns>
    /// <exception cref="TimeoutException">读取超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task<ushort> ReadRegisterAsync(int address, CancellationToken ct)
    {
        var request = ModbusTcpFrame.BuildReadHoldingRegistersRequest(GetNextTransactionId(), GetUnitId(), address, 1);
        var response = await ExecuteWithRetryAsync(request, ct);
        var registers = ParseReadRegistersResponse(response, 1);
        return registers[0];
    }

    /// <summary>
    /// 写入单个保持寄存器（Function Code 06）。
    /// </summary>
    /// <param name="address">寄存器地址（0-based）。</param>
    /// <param name="value">要写入的无符号 16 位整数值。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="TimeoutException">写入超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task WriteRegisterAsync(int address, ushort value, CancellationToken ct)
    {
        var request = ModbusTcpFrame.BuildWriteSingleRegisterRequest(GetNextTransactionId(), GetUnitId(), address, value);
        await ExecuteWithRetryAsync(request, ct);
    }

    /// <summary>
    /// 读取多个连续的保持寄存器（Function Code 03）。
    /// </summary>
    /// <param name="startAddress">起始寄存器地址（0-based）。</param>
    /// <param name="count">要读取的寄存器数量（范围 1 ~ 125）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>寄存器值的无符号 16 位整数数组。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> 超出有效范围时抛出。</exception>
    /// <exception cref="TimeoutException">读取超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task<ushort[]> ReadMultiRegisterAsync(int startAddress, int count, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 125);

        var request = ModbusTcpFrame.BuildReadHoldingRegistersRequest(GetNextTransactionId(), GetUnitId(), startAddress, count);
        var response = await ExecuteWithRetryAsync(request, ct);
        return ParseReadRegistersResponse(response, count);
    }

    /// <summary>
    /// 写入多个连续的保持寄存器（Function Code 16 / 0x10）。
    /// </summary>
    /// <param name="startAddress">起始寄存器地址（0-based）。</param>
    /// <param name="values">要写入的无符号 16 位整数值数组。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="values"/> 数组为空或超出最大长度时抛出。</exception>
    /// <exception cref="TimeoutException">写入超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task WriteMultiRegisterAsync(int startAddress, ushort[] values, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfZero(values.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, 123);

        var request = ModbusTcpFrame.BuildWriteMultipleRegistersRequest(
            GetNextTransactionId(), GetUnitId(), startAddress, values);
        await ExecuteWithRetryAsync(request, ct);
    }

    /// <summary>
    /// 读取多个连续的线圈（Function Code 01）。
    /// </summary>
    /// <param name="startAddress">起始线圈地址（0-based）。</param>
    /// <param name="count">要读取的线圈数量。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>线圈状态布尔数组。</returns>
    /// <exception cref="TimeoutException">读取超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task<bool[]> ReadCoilsAsync(int startAddress, int count, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var request = ModbusTcpFrame.BuildReadCoilsRequest(GetNextTransactionId(), GetUnitId(), startAddress, count);
        var response = await ExecuteWithRetryAsync(request, ct);
        var rawBits = ParseReadResponse(response, count);
        return rawBits.Select(b => b == 1).ToArray();
    }

    /// <summary>
    /// 读取多个离散输入（Function Code 02）。
    /// </summary>
    /// <param name="startAddress">起始离散输入地址（0-based）。</param>
    /// <param name="count">要读取的输入数量。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>离散输入状态布尔数组。</returns>
    /// <exception cref="TimeoutException">读取超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task<bool[]> ReadDiscreteInputsAsync(int startAddress, int count, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var request = ModbusTcpFrame.BuildReadDiscreteInputsRequest(GetNextTransactionId(), GetUnitId(), startAddress, count);
        var response = await ExecuteWithRetryAsync(request, ct);
        var rawBits = ParseReadResponse(response, count);
        return rawBits.Select(b => b == 1).ToArray();
    }

    /// <summary>
    /// 读取多个输入寄存器（Function Code 04）。
    /// </summary>
    /// <param name="startAddress">起始输入寄存器地址（0-based）。</param>
    /// <param name="count">要读取的寄存器数量。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>输入寄存器值的无符号 16 位整数数组。</returns>
    /// <exception cref="TimeoutException">读取超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task<ushort[]> ReadInputRegistersAsync(int startAddress, int count, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 125);

        var request = ModbusTcpFrame.BuildReadInputRegistersRequest(GetNextTransactionId(), GetUnitId(), startAddress, count);
        var response = await ExecuteWithRetryAsync(request, ct);
        return ParseReadRegistersResponse(response, count);
    }

    /// <summary>
    /// 写入多个线圈（Function Code 15 / 0x0F）。
    /// </summary>
    /// <param name="startAddress">起始线圈地址（0-based）。</param>
    /// <param name="values">要写入的线圈状态布尔数组。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> 为 null 时抛出。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="values"/> 数组为空或超出最大长度时抛出。</exception>
    /// <exception cref="TimeoutException">写入超时时抛出。</exception>
    /// <exception cref="ModbusProtocolException">设备返回异常码时抛出。</exception>
    public async Task WriteMultiCoilsAsync(int startAddress, bool[] values, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfZero(values.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(values.Length, 2000);

        var request = ModbusTcpFrame.BuildWriteMultipleCoilsRequest(
            GetNextTransactionId(), GetUnitId(), startAddress, values);
        await ExecuteWithRetryAsync(request, ct);
    }

    // ──────────────────────────── IDisposable ────────────────────────────

    /// <summary>
    /// 释放所有 TCP 资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _networkStream?.Dispose();
            _networkStream = null;
            try { _tcpClient?.Close(); } catch (SocketException) { }
            _tcpClient?.Dispose();
            _tcpClient = null;
        }

        GC.SuppressFinalize(this);
    }

    // ──────────────────────────── 内部帮助方法 ────────────────────────────

    /// <summary>
    /// 获取下一个单调递增的交易 ID（线程安全）。
    /// </summary>
    private int GetNextTransactionId()
    {
        var id = Interlocked.Increment(ref _nextTransactionId);
        // 防止溢出：归零后从 1 重新开始
        if (id > ushort.MaxValue)
        {
            Interlocked.Exchange(ref _nextTransactionId, 1);
            id = 1;
        }
        return id;
    }

    /// <summary>
    /// 获取已配置的 Modbus 从站单元 ID。
    /// </summary>
    private byte GetUnitId() => (byte)_config.SlaveId;

    /// <summary>
    /// 获取当前连接的有效网络流。如果未连接则抛出异常。
    /// </summary>
    private NetworkStream GetStream()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_networkStream is null || _tcpClient is not { Connected: true })
                throw new InvalidOperationException("Modbus TCP 未连接。请先调用 ConnectAsync。");
            return _networkStream;
        }
    }

    /// <summary>
    /// 执行携带重试逻辑的请求-响应交换。
    /// 在临时性失败（超时、IOException）时根据 <see cref="IOConnectionConfig.RetryCount"/> 自动重试。
    /// </summary>
    private async Task<byte[]> ExecuteWithRetryAsync(byte[] request, CancellationToken ct)
    {
        var retryCount = Math.Max(0, _config.RetryCount);
        var lastException = default(Exception);

        for (var attempt = 0; attempt <= retryCount; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                return await ExecuteOnceAsync(request, ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // 由内部超时导致的 OperationCanceledException：当作重试
                lastException = new TimeoutException("Modbus TCP 请求超时。");
            }
            catch (TimeoutException ex)
            {
                lastException = ex;
            }
            catch (IOException ex)
            {
                lastException = ex;
            }
            catch (SocketException ex)
            {
                lastException = ex;
            }
            // ModbusProtocolException 和 ArgumentException 不重试——立即重新抛出
        }

        throw new TimeoutException(
            $"Modbus TCP 操作在重试 {retryCount} 次后仍然失败。", lastException);
    }

    /// <summary>
    /// 执行单次 Modbus TCP 请求-响应交换：
    /// 写入请求帧 → 读取完整响应帧 → 解析 MBAP + PDU。
    /// </summary>
    private async Task<byte[]> ExecuteOnceAsync(byte[] request, CancellationToken ct)
    {
        var stream = GetStream();
        var transactionId = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(0, 2));

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_config.TimeoutMs);

        // ── 发送 ──
        await stream.WriteAsync(request, timeoutCts.Token);
        await stream.FlushAsync(timeoutCts.Token);

        // ── 接收 MBAP 头部（7 字节：2+2+2+1）──
        var headerBuffer = await ReadExactAsync(stream, 7, timeoutCts.Token);

        var responseTransactionId = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(0, 2));
        var protocolId = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(2, 2));
        var remainingLength = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.AsSpan(4, 2));
        var responseUnitId = headerBuffer[6];

        if (protocolId != 0)
            throw new ModbusProtocolException(
                $"Modbus TCP 协议标识符无效：期望 0x0000，收到 0x{protocolId:X4}。");

        // ── 接收 PDU 剩余部分 ──
        var pduLength = remainingLength - 1; // 减去已读的 UnitId (1 字节)
        var pduBuffer = await ReadExactAsync(stream, pduLength, timeoutCts.Token);

        // ── 检查异常码 ──
        var functionCode = pduBuffer[0];
        if ((functionCode & 0x80) != 0)
        {
            var exceptionCode = pduBuffer[1];
            throw new ModbusProtocolException(
                $"Modbus 异常：功能码 0x{functionCode & 0x7F:X2}，异常码 0x{exceptionCode:X2}",
                exceptionCode);
        }

        return pduBuffer;
    }

    /// <summary>
    /// 从 <see cref="NetworkStream"/> 中精确读取指定数量的字节。
    /// </summary>
    private static async Task<byte[]> ReadExactAsync(
        NetworkStream stream, int count, CancellationToken ct)
    {
        var buffer = new byte[count];
        var offset = 0;

        while (offset < count)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
            if (bytesRead == 0)
                throw new IOException(
                    $"Modbus TCP 连接意外关闭。需要读取 {count} 字节，"
                    + $"已读取 {offset} 字节。");
            offset += bytesRead;
        }

        return buffer;
    }

    /// <summary>
    /// 解析读取线圈 / 离散输入响应（FC01 / FC02）。
    /// </summary>
    private static byte[] ParseReadResponse(byte[] pduBuffer, int expectedBitCount)
    {
        var byteCount = pduBuffer[1];
        var rawBytes = pduBuffer.AsSpan(2, byteCount);
        var result = new byte[expectedBitCount];

        for (var i = 0; i < expectedBitCount; i++)
        {
            var byteIndex = i / 8;
            var bitIndex = i % 8;
            result[i] = byteIndex < rawBytes.Length
                ? (byte)((rawBytes[byteIndex] >> bitIndex) & 1)
                : (byte)0;
        }

        return result;
    }

    /// <summary>
    /// 解析读取寄存器响应（FC03 / FC04）。
    /// </summary>
    private static ushort[] ParseReadRegistersResponse(byte[] pduBuffer, int expectedCount)
    {
        var byteCount = pduBuffer[1];
        var data = pduBuffer.AsSpan(2, byteCount);
        var registers = new ushort[expectedCount];

        for (var i = 0; i < expectedCount; i++)
            registers[i] = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(i * 2, 2));

        return registers;
    }

    // ──────────────────────────── ModbusTcpFrame ────────────────────────────

    /// <summary>
    /// Modbus TCP 协议帧构建器。所有方法均为静态，返回完整的 Modbus TCP 请求帧字节数组。
    /// 帧结构：MBAP Header (7字节) + PDU。
    /// </summary>
    public static class ModbusTcpFrame
    {
        private const int MbapHeaderLength = 7;
        private const ushort ProtocolId = 0x0000;

        // ── FC01: Read Coils ──

        /// <summary>
        /// 构建读取线圈请求帧（Function Code 01）。
        /// </summary>
        /// <param name="transactionId">Modbus TCP 事务标识符。</param>
        /// <param name="unitId">从站单元 ID。</param>
        /// <param name="startAddress">起始线圈地址（0-based）。</param>
        /// <param name="count">要读取的线圈数量。</param>
        /// <returns>完整的 Modbus TCP 请求帧字节数组。</returns>
        public static byte[] BuildReadCoilsRequest(
            int transactionId, byte unitId, int startAddress, int count)
        {
            // PDU: FC(1) + StartAddress(2) + Quantity(2) = 5
            const int pduSize = 5;
            var frame = new byte[MbapHeaderLength + pduSize];
            FillMbapHeader(frame, (ushort)transactionId, unitId, (ushort)(pduSize + 1));
            frame[7] = 0x01;                                          // Function Code
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8, 2), (ushort)startAddress); // Starting Address
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(10, 2), (ushort)count);         // Quantity of Coils
            return frame;
        }

        // ── FC02: Read Discrete Inputs ──

        /// <summary>
        /// 构建读取离散输入请求帧（Function Code 02）。
        /// </summary>
        /// <param name="transactionId">Modbus TCP 事务标识符。</param>
        /// <param name="unitId">从站单元 ID。</param>
        /// <param name="startAddress">起始离散输入地址（0-based）。</param>
        /// <param name="count">要读取的离散输入数量。</param>
        /// <returns>完整的 Modbus TCP 请求帧字节数组。</returns>
        public static byte[] BuildReadDiscreteInputsRequest(
            int transactionId, byte unitId, int startAddress, int count)
        {
            const int pduSize = 5;
            var frame = new byte[MbapHeaderLength + pduSize];
            FillMbapHeader(frame, (ushort)transactionId, unitId, (ushort)(pduSize + 1));
            frame[7] = 0x02;
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8, 2), (ushort)startAddress);
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(10, 2), (ushort)count);
            return frame;
        }

        // ── FC03: Read Holding Registers ──

        /// <summary>
        /// 构建读取保持寄存器请求帧（Function Code 03）。
        /// </summary>
        /// <param name="transactionId">Modbus TCP 事务标识符。</param>
        /// <param name="unitId">从站单元 ID。</param>
        /// <param name="startAddress">起始寄存器地址（0-based）。</param>
        /// <param name="count">要读取的寄存器数量。</param>
        /// <returns>完整的 Modbus TCP 请求帧字节数组。</returns>
        public static byte[] BuildReadHoldingRegistersRequest(
            int transactionId, byte unitId, int startAddress, int count)
        {
            const int pduSize = 5;
            var frame = new byte[MbapHeaderLength + pduSize];
            FillMbapHeader(frame, (ushort)transactionId, unitId, (ushort)(pduSize + 1));
            frame[7] = 0x03;
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8, 2), (ushort)startAddress);
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(10, 2), (ushort)count);
            return frame;
        }

        // ── FC04: Read Input Registers ──

        /// <summary>
        /// 构建读取输入寄存器请求帧（Function Code 04）。
        /// </summary>
        /// <param name="transactionId">Modbus TCP 事务标识符。</param>
        /// <param name="unitId">从站单元 ID。</param>
        /// <param name="startAddress">起始输入寄存器地址（0-based）。</param>
        /// <param name="count">要读取的寄存器数量。</param>
        /// <returns>完整的 Modbus TCP 请求帧字节数组。</returns>
        public static byte[] BuildReadInputRegistersRequest(
            int transactionId, byte unitId, int startAddress, int count)
        {
            const int pduSize = 5;
            var frame = new byte[MbapHeaderLength + pduSize];
            FillMbapHeader(frame, (ushort)transactionId, unitId, (ushort)(pduSize + 1));
            frame[7] = 0x04;
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8, 2), (ushort)startAddress);
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(10, 2), (ushort)count);
            return frame;
        }

        // ── FC05: Write Single Coil ──

        /// <summary>
        /// 构建写入单个线圈请求帧（Function Code 05）。
        /// </summary>
        /// <param name="transactionId">Modbus TCP 事务标识符。</param>
        /// <param name="unitId">从站单元 ID。</param>
        /// <param name="address">线圈地址（0-based）。</param>
        /// <param name="value">线圈值：true = 0xFF00（ON），false = 0x0000（OFF）。</param>
        /// <returns>完整的 Modbus TCP 请求帧字节数组。</returns>
        public static byte[] BuildWriteSingleCoilRequest(
            int transactionId, byte unitId, int address, bool value)
        {
            // PDU: FC(1) + Address(2) + Value(2) = 5
            const int pduSize = 5;
            var frame = new byte[MbapHeaderLength + pduSize];
            FillMbapHeader(frame, (ushort)transactionId, unitId, (ushort)(pduSize + 1));
            frame[7] = 0x05;
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8, 2), (ushort)address);
            // Modbus 规范：ON = 0xFF00, OFF = 0x0000
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(10, 2), value ? (ushort)0xFF00 : (ushort)0x0000);
            return frame;
        }

        // ── FC06: Write Single Register ──

        /// <summary>
        /// 构建写入单个保持寄存器请求帧（Function Code 06）。
        /// </summary>
        /// <param name="transactionId">Modbus TCP 事务标识符。</param>
        /// <param name="unitId">从站单元 ID。</param>
        /// <param name="address">寄存器地址（0-based）。</param>
        /// <param name="value">要写入的 16 位无符号整数值。</param>
        /// <returns>完整的 Modbus TCP 请求帧字节数组。</returns>
        public static byte[] BuildWriteSingleRegisterRequest(
            int transactionId, byte unitId, int address, ushort value)
        {
            const int pduSize = 5;
            var frame = new byte[MbapHeaderLength + pduSize];
            FillMbapHeader(frame, (ushort)transactionId, unitId, (ushort)(pduSize + 1));
            frame[7] = 0x06;
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8, 2), (ushort)address);
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(10, 2), value);
            return frame;
        }

        // ── FC15 (0x0F): Write Multiple Coils ──

        /// <summary>
        /// 构建写入多个线圈请求帧（Function Code 15 / 0x0F）。
        /// </summary>
        /// <param name="transactionId">Modbus TCP 事务标识符。</param>
        /// <param name="unitId">从站单元 ID。</param>
        /// <param name="startAddress">起始线圈地址（0-based）。</param>
        /// <param name="values">要写入的线圈状态布尔数组。</param>
        /// <returns>完整的 Modbus TCP 请求帧字节数组。</returns>
        public static byte[] BuildWriteMultipleCoilsRequest(
            int transactionId, byte unitId, int startAddress, bool[] values)
        {
            var quantity = (ushort)values.Length;
            var byteCount = (quantity + 7) / 8; // 向上取整
            // PDU: FC(1) + StartAddress(2) + Quantity(2) + ByteCount(1) + Outputs(n)
            var pduSize = 1 + 2 + 2 + 1 + byteCount;
            var frame = new byte[MbapHeaderLength + pduSize];

            FillMbapHeader(frame, (ushort)transactionId, unitId, (ushort)(pduSize + 1));
            frame[7] = 0x0F;                                                   // Function Code 15
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8, 2), (ushort)startAddress); // Starting Address
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(10, 2), quantity);             // Quantity of Outputs
            frame[12] = (byte)byteCount;                                        // Byte Count

            // 打包布尔值到位
            for (var i = 0; i < quantity; i++)
            {
                if (values[i])
                    frame[13 + i / 8] |= (byte)(1 << (i % 8));
            }

            return frame;
        }

        // ── FC16 (0x10): Write Multiple Registers ──

        /// <summary>
        /// 构建写入多个保持寄存器请求帧（Function Code 16 / 0x10）。
        /// </summary>
        /// <param name="transactionId">Modbus TCP 事务标识符。</param>
        /// <param name="unitId">从站单元 ID。</param>
        /// <param name="startAddress">起始寄存器地址（0-based）。</param>
        /// <param name="values">要写入的 16 位无符号整数值数组。</param>
        /// <returns>完整的 Modbus TCP 请求帧字节数组。</returns>
        public static byte[] BuildWriteMultipleRegistersRequest(
            int transactionId, byte unitId, int startAddress, ushort[] values)
        {
            var quantity = (ushort)values.Length;
            var byteCount = (byte)(quantity * 2);
            // PDU: FC(1) + StartAddress(2) + Quantity(2) + ByteCount(1) + Registers(n*2)
            var pduSize = 1 + 2 + 2 + 1 + byteCount;
            var frame = new byte[MbapHeaderLength + pduSize];

            FillMbapHeader(frame, (ushort)transactionId, unitId, (ushort)(pduSize + 1));
            frame[7] = 0x10;                                                   // Function Code 16
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8, 2), (ushort)startAddress); // Starting Address
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(10, 2), quantity);             // Quantity of Registers
            frame[12] = byteCount;                                              // Byte Count

            for (var i = 0; i < quantity; i++)
                BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(13 + i * 2, 2), values[i]);

            return frame;
        }

        // ── MBAP Header Helper ──

        /// <summary>
        /// 在帧起始位置填充 Modbus TCP MBAP 头部（7 字节）。
        /// </summary>
        private static void FillMbapHeader(byte[] frame, ushort transactionId, byte unitId, ushort length)
        {
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(0, 2), transactionId); // Transaction ID
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(2, 2), ProtocolId);    // Protocol ID (0x0000)
            BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(4, 2), length);        // Length (PDU长度 + UnitId 1字节)
            frame[6] = unitId;                                                        // Unit ID
        }
    }
}

/// <summary>
/// Modbus 协议异常。当 Modbus 从站设备返回异常响应时抛出。
/// </summary>
public sealed class ModbusProtocolException : Exception
{
    /// <summary>获取 Modbus 异常码。</summary>
    public byte ExceptionCode { get; }

    /// <summary>
    /// 使用指定的异常描述和异常码初始化 <see cref="ModbusProtocolException"/>。
    /// </summary>
    /// <param name="message">异常描述信息。</param>
    /// <param name="exceptionCode">Modbus 异常码。</param>
    public ModbusProtocolException(string message, byte exceptionCode = 0)
        : base(GetExceptionMessage(message, exceptionCode))
    {
        ExceptionCode = exceptionCode;
    }

    private static string GetExceptionMessage(string message, byte exceptionCode)
    {
        return exceptionCode switch
        {
            0x01 => $"{message} —— 非法功能码（Illegal Function）。",
            0x02 => $"{message} —— 非法数据地址（Illegal Data Address）。",
            0x03 => $"{message} —— 非法数据值（Illegal Data Value）。",
            0x04 => $"{message} —— 从站设备故障（Slave Device Failure）。",
            0x05 => $"{message} —— 确认（Acknowledge）。",
            0x06 => $"{message} —— 从站设备忙（Slave Device Busy）。",
            0x08 => $"{message} —— 内存奇偶校验错误（Memory Parity Error）。",
            0x0A => $"{message} —— 网关路径不可用（Gateway Path Unavailable）。",
            0x0B => $"{message} —— 网关目标设备无响应（Gateway Target Device Failed to Respond）。",
            _ => message,
        };
    }
}
