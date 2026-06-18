using System.Collections.Concurrent;

namespace Cc.IDE.PLC;

/// <summary>
/// PLC 设备管理中央服务实现。
/// 管理所有 PLC 设备的连接池，提供统一的读写操作入口。
/// </summary>
public sealed class PLCService : IPLCService
{
    /// <summary>
    /// 设备协议实例池，线程安全。
    /// </summary>
    private readonly ConcurrentDictionary<string, IPlcProtocol> _devicePool = new();

    /// <summary>
    /// 设备连接状态跟踪，线程安全。
    /// </summary>
    private readonly ConcurrentDictionary<string, PLCConnectionState> _connectionStates = new();

    /// <summary>
    /// 设备连接配置记录，用于断开后重建状态快照。
    /// </summary>
    private readonly ConcurrentDictionary<string, (IOConnectionConfig Config, string ProtocolType)> _deviceConfigs = new();

    /// <summary>
    /// 当前已连接的设备数量。
    /// </summary>
    public int ConnectedDeviceCount => _devicePool.Count;

    /// <summary>
    /// 获取指定设备是否已连接。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <returns>如果设备已连接则返回 <c>true</c>。</returns>
    public bool IsDeviceConnected(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return false;

        return _devicePool.TryGetValue(deviceId, out var protocol) && protocol.IsConnected;
    }

    /// <summary>
    /// 建立与指定 PLC 设备的连接。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <param name="config">连接配置。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="ArgumentNullException"><paramref name="deviceId"/> 或 <paramref name="config"/> 为 <c>null</c> 或空白。</exception>
    /// <exception cref="InvalidOperationException">设备已存在连接。</exception>
    public async Task ConnectDeviceAsync(string deviceId, IOConnectionConfig config, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentNullException(nameof(deviceId), "设备标识符不能为空。");

        if (config is null)
            throw new ArgumentNullException(nameof(config), "连接配置不能为空。");

        // 检查是否已存在连接
        if (_devicePool.ContainsKey(deviceId))
            throw new InvalidOperationException($"设备 '{deviceId}' 已连接，请先断开再重试。");

        // 确定协议类型
        var protocolType = !string.IsNullOrWhiteSpace(config.PlcProtocol)
            ? config.PlcProtocol
            : "ModbusTcp";

        // 创建协议实例
        var protocol = CreateProtocol(protocolType);

        try
        {
            // 建立连接
            await protocol.ConnectAsync(config, ct).ConfigureAwait(false);

            // 存储到连接池
            _devicePool[deviceId] = protocol;
            _deviceConfigs[deviceId] = (config, protocolType);

            // 更新连接状态
            _connectionStates[deviceId] = new PLCConnectionState
            {
                DeviceId = deviceId,
                IsConnected = true,
                ProtocolType = protocolType,
                Host = config.Host,
                Port = config.Port,
                ConnectedAt = DateTime.UtcNow,
            };
        }
        catch
        {
            // 连接失败时清理部分初始化状态
            (protocol as IDisposable)?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// 断开指定 PLC 设备的连接。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <remarks>如果设备不存在于连接池中，此方法不执行任何操作。</remarks>
    public async Task DisconnectDeviceAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return;

        if (_devicePool.TryRemove(deviceId, out var protocol))
        {
            try
            {
                await protocol.DisconnectAsync().ConfigureAwait(false);
            }
            finally
            {
                (protocol as IDisposable)?.Dispose();
                _deviceConfigs.TryRemove(deviceId, out _);
                _connectionStates.TryRemove(deviceId, out _);
            }
        }
    }

    /// <summary>
    /// 断开所有已连接的 PLC 设备。
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        // 收集所有设备 ID 并分批断开
        var deviceIds = _devicePool.Keys.ToArray();
        var tasks = deviceIds.Select(id => DisconnectDeviceAsync(id));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// 读取指定设备的单个线圈状态。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <param name="address">线圈地址。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>线圈的布尔状态值。</returns>
    /// <exception cref="InvalidOperationException">设备未连接或不存在。</exception>
    public async Task<bool> ReadCoilAsync(string deviceId, int address, CancellationToken ct)
    {
        var protocol = GetProtocolOrThrow(deviceId);
        return await protocol.ReadCoilAsync(address, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 向指定设备的线圈写入值。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <param name="address">线圈地址。</param>
    /// <param name="value">要写入的布尔值。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="InvalidOperationException">设备未连接或不存在。</exception>
    public async Task WriteCoilAsync(string deviceId, int address, bool value, CancellationToken ct)
    {
        var protocol = GetProtocolOrThrow(deviceId);
        await protocol.WriteCoilAsync(address, value, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 读取指定设备的单个保持寄存器。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <param name="address">寄存器地址。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>寄存器的无符号 16 位整数值。</returns>
    /// <exception cref="InvalidOperationException">设备未连接或不存在。</exception>
    public async Task<ushort> ReadRegisterAsync(string deviceId, int address, CancellationToken ct)
    {
        var protocol = GetProtocolOrThrow(deviceId);
        return await protocol.ReadRegisterAsync(address, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 向指定设备的保持寄存器写入值。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <param name="address">寄存器地址。</param>
    /// <param name="value">要写入的无符号 16 位整数值。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="InvalidOperationException">设备未连接或不存在。</exception>
    public async Task WriteRegisterAsync(string deviceId, int address, ushort value, CancellationToken ct)
    {
        var protocol = GetProtocolOrThrow(deviceId);
        await protocol.WriteRegisterAsync(address, value, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 读取指定设备的多个连续保持寄存器。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <param name="startAddress">起始寄存器地址。</param>
    /// <param name="count">要读取的寄存器数量。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>寄存器值的无符号 16 位整数数组。</returns>
    /// <exception cref="InvalidOperationException">设备未连接或不存在。</exception>
    public async Task<ushort[]> ReadMultiRegisterAsync(string deviceId, int startAddress, int count, CancellationToken ct)
    {
        var protocol = GetProtocolOrThrow(deviceId);
        return await protocol.ReadMultiRegisterAsync(startAddress, count, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 向指定设备的多个连续保持寄存器写入值。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <param name="startAddress">起始寄存器地址。</param>
    /// <param name="values">要写入的无符号 16 位整数值数组。</param>
    /// <exception cref="InvalidOperationException">设备未连接或不存在。</exception>
    public async Task WriteMultiRegisterAsync(string deviceId, int startAddress, ushort[] values, CancellationToken ct)
    {
        var protocol = GetProtocolOrThrow(deviceId);
        await protocol.WriteMultiRegisterAsync(startAddress, values, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取所有设备的连接状态快照。
    /// </summary>
    /// <returns>设备连接状态的只读字典。</returns>
    public IReadOnlyDictionary<string, PLCConnectionState> GetConnectionStates()
    {
        return new Dictionary<string, PLCConnectionState>(_connectionStates);
    }

    /// <summary>
    /// 根据协议类型创建对应的协议实例。
    /// </summary>
    /// <param name="protocolType">协议类型字符串（如 "ModbusTcp"）。</param>
    /// <returns>对应协议的 <see cref="IPlcProtocol"/> 实例。</returns>
    /// <exception cref="ArgumentException">不支持的协议类型。</exception>
    public IPlcProtocol CreateProtocol(string protocolType)
    {
        return protocolType switch
        {
            "ModbusTcp" => new ModbusTcpProtocol(),
            // 未来支持更多协议类型：
            // "ModbusRtu"  => new ModbusRtuProtocol(),
            // "SiemensS7"  => new SiemensS7Protocol(),
            // "MitsubishiMC" => new MitsubishiMCProtocol(),
            _ => throw new ArgumentException($"不支持的协议类型 '{protocolType}'。当前支持的协议: ModbusTcp。", nameof(protocolType)),
        };
    }

    /// <summary>
    /// 从连接池获取指定设备的协议实例，如果不存在则抛出异常。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <returns>协议实例。</returns>
    /// <exception cref="InvalidOperationException">设备未连接或不存在。</exception>
    private IPlcProtocol GetProtocolOrThrow(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentNullException(nameof(deviceId), "设备标识符不能为空。");

        if (!_devicePool.TryGetValue(deviceId, out var protocol))
            throw new InvalidOperationException($"设备 '{deviceId}' 未连接或不存在，请先调用 ConnectDeviceAsync 建立连接。");

        if (!protocol.IsConnected)
            throw new InvalidOperationException($"设备 '{deviceId}' 的连接已断开。");

        return protocol;
    }
}
