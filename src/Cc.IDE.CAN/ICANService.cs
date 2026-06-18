namespace Cc.IDE.CAN;

/// <summary>
/// CAN 总线设备管理中央服务接口。
/// 管理所有 CAN 接口实例，提供统一的帧发送/接收和健康检查入口。
/// </summary>
public interface ICANService
{
    /// <summary>
    /// 当前已连接的 CAN 接口数量。
    /// </summary>
    int ConnectedInterfaceCount { get; }

    /// <summary>
    /// 获取指定名称的 CAN 接口是否已连接。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    bool IsInterfaceConnected(string interfaceName);

    /// <summary>
    /// 连接指定名称的 CAN 接口。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="interfaceType">接口类型（"PCAN"、"SocketCAN"、"Kvaser"）。</param>
    /// <param name="ct">取消令牌。</param>
    Task ConnectInterfaceAsync(string interfaceName, string interfaceType, CancellationToken ct);

    /// <summary>
    /// 断开指定名称的 CAN 接口。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    Task DisconnectInterfaceAsync(string interfaceName);

    /// <summary>
    /// 断开所有 CAN 接口。
    /// </summary>
    Task DisconnectAllAsync();

    /// <summary>
    /// 向指定 CAN 接口发送帧。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="frame">要发送的 CAN 帧。</param>
    /// <param name="ct">取消令牌。</param>
    Task SendFrameAsync(string interfaceName, CanFrame frame, CancellationToken ct);

    /// <summary>
    /// 从指定 CAN 接口读取已接收的帧。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已接收的 CAN 帧只读列表。</returns>
    Task<IReadOnlyList<CanFrame>> ReadAsync(string interfaceName, CancellationToken ct);

    /// <summary>
    /// 获取所有 CAN 接口的连接状态快照。
    /// </summary>
    IReadOnlyDictionary<string, CANInterfaceState> GetInterfaceStates();

    /// <summary>
    /// 根据接口类型创建对应的 CAN 接口实例。
    /// </summary>
    /// <param name="interfaceType">接口类型（"PCAN"、"SocketCAN"、"Kvaser"）。</param>
    /// <returns>对应的 <see cref="ICanInterface"/> 实例。</returns>
    ICanInterface CreateInterface(string interfaceType);

    /// <summary>
    /// 订阅指定 CAN 接口的帧接收事件。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="handler">接收到帧时的回调。</param>
    void SubscribeFrames(string interfaceName, Action<CanFrame> handler);

    /// <summary>
    /// 取消订阅指定 CAN 接口的帧接收事件。
    /// </summary>
    /// <param name="interfaceName">接口名称。</param>
    /// <param name="handler">要移除的回调。</param>
    void UnsubscribeFrames(string interfaceName, Action<CanFrame> handler);
}

/// <summary>
/// CAN 接口连接状态快照。
/// </summary>
public sealed class CANInterfaceState
{
    /// <summary>接口名称。</summary>
    public string InterfaceName { get; set; } = string.Empty;
    /// <summary>接口类型。</summary>
    public string InterfaceType { get; set; } = string.Empty;
    /// <summary>是否已连接。</summary>
    public bool IsConnected { get; set; }
    /// <summary>最近连接时间。</summary>
    public DateTime? ConnectedAt { get; set; }
}
