namespace Cc.IDE.PLC;

/// <summary>
/// 统一 IO 服务抽象接口。
/// 将 PLC IO 和 CAN IO 抽象为统一的读写操作模型（架构 §8）。
/// </summary>
public interface IIOService
{
    /// <summary>
    /// 服务类型标识（"PLC" 或 "CAN"）。
    /// </summary>
    string ServiceType { get; }

    /// <summary>
    /// 解析逻辑点位代码为物理地址信息。
    /// </summary>
    /// <param name="pointCode">逻辑点位代码（如 "D100"、"Y0"）。</param>
    /// <returns>物理地址解析结果。</returns>
    IOPointResolution ResolvePoint(string pointCode);

    /// <summary>
    /// 读取指定点位的当前值。
    /// </summary>
    /// <param name="deviceId">目标设备标识符。</param>
    /// <param name="pointCode">点位代码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>点位的当前值。</returns>
    Task<object> ReadAsync(string deviceId, string pointCode, CancellationToken ct);

    /// <summary>
    /// 向指定点位写入值。
    /// </summary>
    /// <param name="deviceId">目标设备标识符。</param>
    /// <param name="pointCode">点位代码。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="ct">取消令牌。</param>
    Task WriteAsync(string deviceId, string pointCode, object value, CancellationToken ct);

    /// <summary>
    /// 等待点位达到目标值。
    /// </summary>
    /// <param name="deviceId">目标设备标识符。</param>
    /// <param name="pointCode">点位代码。</param>
    /// <param name="targetValue">期望的目标值。</param>
    /// <param name="timeoutMs">最大等待时间（毫秒）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>如果在超时前达到目标值则返回 <c>true</c>。</returns>
    Task<bool> WaitForAsync(string deviceId, string pointCode, object targetValue, int timeoutMs, CancellationToken ct);
}

/// <summary>
/// IO 点位物理地址解析结果。
/// </summary>
public sealed class IOPointResolution
{
    /// <summary>寄存器类型（Coil/DiscreteInput/HoldingRegister/InputRegister）。</summary>
    public string RegisterKind { get; set; } = string.Empty;
    /// <summary>寄存器偏移地址。</summary>
    public int RegisterOffset { get; set; }
    /// <summary>位索引（-1 表示非位类型）。</summary>
    public int BitIndex { get; set; } = -1;
    /// <summary>数据类型（Bool/UInt16/Int16/Float32）。</summary>
    public string DataType { get; set; } = "Bool";
    /// <summary>访问权限（Read/Write/ReadWrite）。</summary>
    public string Access { get; set; } = "ReadWrite";
}

/// <summary>
/// IO 点位代码解析器接口。
/// 不同协议实现各自点位代码的解析逻辑。
/// </summary>
public interface IIOPointResolver
{
    /// <summary>
    /// 解析点位代码字符串为物理地址信息。
    /// </summary>
    /// <param name="pointCode">点位代码（如 "D100"、"X0"、"Y5"）。</param>
    /// <returns>解析后的物理地址信息；无法解析时返回 <c>null</c>。</returns>
    IOPointResolution? Resolve(string pointCode);
}
