namespace Cc.IDE.PLC;

/// <summary>
/// PLC 协议抽象。实现类处理 Modbus TCP/RTU、Siemens S7、Mitsubishi MC 等协议。
/// </summary>
public interface IPlcProtocol
{
    /// <summary>获取一个值，指示是否已连接到 PLC 设备。</summary>
    bool IsConnected { get; }

    /// <summary>
    /// 连接到 PLC 设备。
    /// </summary>
    /// <param name="config">连接配置参数，包含主机地址、端口、协议等。</param>
    /// <param name="ct">取消令牌，用于取消连接操作。</param>
    Task ConnectAsync(IOConnectionConfig config, CancellationToken ct);

    /// <summary>
    /// 断开与 PLC 设备的连接。
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 读取单个线圈（数字输出状态）。
    /// </summary>
    /// <param name="address">线圈地址。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>线圈的布尔状态值。</returns>
    Task<bool> ReadCoilAsync(int address, CancellationToken ct);

    /// <summary>
    /// 写入单个线圈。
    /// </summary>
    /// <param name="address">线圈地址。</param>
    /// <param name="value">要写入的布尔值。</param>
    /// <param name="ct">取消令牌。</param>
    Task WriteCoilAsync(int address, bool value, CancellationToken ct);

    /// <summary>
    /// 读取单个保持寄存器。
    /// </summary>
    /// <param name="address">寄存器地址。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>寄存器的无符号 16 位整数值。</returns>
    Task<ushort> ReadRegisterAsync(int address, CancellationToken ct);

    /// <summary>
    /// 写入单个保持寄存器。
    /// </summary>
    /// <param name="address">寄存器地址。</param>
    /// <param name="value">要写入的无符号 16 位整数值。</param>
    /// <param name="ct">取消令牌。</param>
    Task WriteRegisterAsync(int address, ushort value, CancellationToken ct);

    /// <summary>
    /// 读取多个连续的保持寄存器。
    /// </summary>
    /// <param name="startAddress">起始寄存器地址。</param>
    /// <param name="count">要读取的寄存器数量。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>寄存器值的无符号 16 位整数数组。</returns>
    Task<ushort[]> ReadMultiRegisterAsync(int startAddress, int count, CancellationToken ct);

    /// <summary>
    /// 写入多个连续的保持寄存器。
    /// </summary>
    /// <param name="startAddress">起始寄存器地址。</param>
    /// <param name="values">要写入的无符号 16 位整数值数组。</param>
    /// <param name="ct">取消令牌。</param>
    Task WriteMultiRegisterAsync(int startAddress, ushort[] values, CancellationToken ct);
}

/// <summary>
/// 最小化的 IO 连接配置（镜像 ProjectSystem 模型以避免依赖）。
/// </summary>
public sealed class IOConnectionConfig
{
    // PLC
    /// <summary>PLC 协议类型（如 ModbusTcp、ModbusRtu、SiemensS7 等）。</summary>
    public string PlcProtocol { get; set; } = "ModbusTcp";
    /// <summary>PLC 设备的主机地址或 IP。</summary>
    public string Host { get; set; } = "127.0.0.1";
    /// <summary>PLC 设备的 TCP 端口号。</summary>
    public int Port { get; set; } = 502;
    /// <summary>Modbus 从站 ID。</summary>
    public int SlaveId { get; set; } = 1;
    /// <summary>西门子 PLC 机架号。</summary>
    public int Rack { get; set; }
    /// <summary>西门子 PLC 插槽号。</summary>
    public int Slot { get; set; }

    // CAN
    /// <summary>CAN 接口类型（如 PCAN、SocketCAN、Kvaser 等）。</summary>
    public string CanInterface { get; set; } = "PCAN";
    /// <summary>CAN 总线波特率。</summary>
    public int BitRate { get; set; } = 500000;
    /// <summary>CAN 通道号。</summary>
    public int CanChannel { get; set; }

    // General
    /// <summary>轮询间隔（毫秒）。</summary>
    public int PollIntervalMs { get; set; } = 100;
    /// <summary>通信超时时间（毫秒）。</summary>
    public int TimeoutMs { get; set; } = 3000;
    /// <summary>失败重试次数。</summary>
    public int RetryCount { get; set; } = 2;
}
