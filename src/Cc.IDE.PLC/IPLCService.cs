namespace Cc.IDE.PLC;

/// <summary>
/// PLC 设备管理中央服务接口。
/// 管理所有 PLC 设备的连接池，提供统一的读写操作入口。
/// </summary>
public interface IPLCService
{
    /// <summary>
    /// 当前已连接的设备数量。
    /// </summary>
    int ConnectedDeviceCount { get; }

    /// <summary>
    /// 获取指定设备是否已连接。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    bool IsDeviceConnected(string deviceId);

    /// <summary>
    /// 建立与指定 PLC 设备的连接。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    /// <param name="config">连接配置。</param>
    /// <param name="ct">取消令牌。</param>
    Task ConnectDeviceAsync(string deviceId, IOConnectionConfig config, CancellationToken ct);

    /// <summary>
    /// 断开指定 PLC 设备的连接。
    /// </summary>
    /// <param name="deviceId">设备标识符。</param>
    Task DisconnectDeviceAsync(string deviceId);

    /// <summary>
    /// 断开所有已连接的 PLC 设备。
    /// </summary>
    Task DisconnectAllAsync();

    // Pass-through IO operations (delegate to the correct protocol instance)
    Task<bool> ReadCoilAsync(string deviceId, int address, CancellationToken ct);
    Task WriteCoilAsync(string deviceId, int address, bool value, CancellationToken ct);
    Task<ushort> ReadRegisterAsync(string deviceId, int address, CancellationToken ct);
    Task WriteRegisterAsync(string deviceId, int address, ushort value, CancellationToken ct);
    Task<ushort[]> ReadMultiRegisterAsync(string deviceId, int startAddress, int count, CancellationToken ct);
    Task WriteMultiRegisterAsync(string deviceId, int startAddress, ushort[] values, CancellationToken ct);

    /// <summary>
    /// 获取所有设备的连接状态快照。
    /// </summary>
    IReadOnlyDictionary<string, PLCConnectionState> GetConnectionStates();

    /// <summary>
    /// 根据协议类型创建对应的协议实例。
    /// </summary>
    /// <param name="protocolType">协议类型字符串（如 "ModbusTcp"）。</param>
    /// <returns>对应协议的 <see cref="IPlcProtocol"/> 实例。</returns>
    IPlcProtocol CreateProtocol(string protocolType);
}

/// <summary>
/// PLC 设备连接状态快照。
/// </summary>
public sealed class PLCConnectionState
{
    /// <summary>设备标识符。</summary>
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>是否已连接。</summary>
    public bool IsConnected { get; set; }
    /// <summary>协议类型。</summary>
    public string ProtocolType { get; set; } = string.Empty;
    /// <summary>连接主机地址。</summary>
    public string Host { get; set; } = string.Empty;
    /// <summary>连接端口号。</summary>
    public int Port { get; set; }
    /// <summary>最近连接时间。</summary>
    public DateTime? ConnectedAt { get; set; }
}
