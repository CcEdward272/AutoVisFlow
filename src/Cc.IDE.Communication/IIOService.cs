namespace Cc.IDE.Communication;

/// <summary>
/// IO 服务抽象接口。
/// 定义统一的数据读写操作，PLCIO、CAN IO 等不同底层实现通过此接口对外暴露一致的操作方式。
/// </summary>
/// <remarks>
/// <para>
/// 此为"点解析-读写"两层抽象：
/// </para>
/// <list type="bullet">
///   <item><see cref="ResolvePoint"/> — 将字符串格式的 IO 点标识解析为硬件层面的位号/地址。</item>
///   <item><see cref="ReadAsync"/> / <see cref="WriteAsync"/> — 对解析后的 IO 点执行数据读写。</item>
/// </list>
/// </remarks>
public interface IIOService
{
    /// <summary>
    /// 服务类型标识，如 "PLC"、"CAN"。
    /// </summary>
    string ServiceType { get; }

    /// <summary>
    /// 将字符串格式的 IO 点标识解析为可供硬件操作的 <see cref="IOPointResolution"/>。
    /// </summary>
    /// <param name="pointCode">IO 点标识字符串（如 PLC 的 "%IX1.2"、CAN 的 "0x201:3:0"）。</param>
    /// <returns>解析后的 IO 点信息，包含位地址和数据范围。</returns>
    /// <exception cref="FormatException">点标识格式无法解析时抛出。</exception>
    IOPointResolution ResolvePoint(string pointCode);

    /// <summary>
    /// 从指定 IO 点读取数据。
    /// </summary>
    /// <param name="resolution">由 <see cref="ResolvePoint"/> 返回的 IO 点解析结果。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>IO 点的当前数据字节数组。</returns>
    Task<byte[]> ReadAsync(IOPointResolution resolution, CancellationToken ct);

    /// <summary>
    /// 向指定 IO 点写入数据。
    /// </summary>
    /// <param name="resolution">由 <see cref="ResolvePoint"/> 返回的 IO 点解析结果。</param>
    /// <param name="data">要写入的数据字节数组。</param>
    /// <param name="ct">取消令牌。</param>
    Task WriteAsync(IOPointResolution resolution, byte[] data, CancellationToken ct);

    /// <summary>
    /// 等待指定 IO 点达到期望值（轮询模式）。
    /// </summary>
    /// <param name="resolution">由 <see cref="ResolvePoint"/> 返回的 IO 点解析结果。</param>
    /// <param name="expectedValue">期望的字节值。</param>
    /// <param name="timeout">超时时间。</param>
    /// <param name="pollInterval">轮询间隔。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>若在超时内达到期望值则为 <c>true</c>，否则为 <c>false</c>。</returns>
    Task<bool> WaitForAsync(IOPointResolution resolution, byte[] expectedValue, TimeSpan timeout, TimeSpan pollInterval, CancellationToken ct);
}

/// <summary>
/// IO 点解析结果，包含硬件层面的位地址和数据范围信息。
/// </summary>
public sealed class IOPointResolution
{
    /// <summary>
    /// 硬件位地址（如线圈地址、CAN ID、寄存器地址）。
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// 子地址（如字节偏移、位偏移）。默认值为 -1 表示不适用。
    /// </summary>
    public int SubAddress { get; set; } = -1;

    /// <summary>
    /// 位偏移（当需要访问单个位时使用）。默认值为 -1 表示不适用。
    /// </summary>
    public int BitOffset { get; set; } = -1;

    /// <summary>
    /// 数据长度（字节数）。
    /// </summary>
    public int DataLength { get; set; } = 1;

    /// <summary>
    /// 可选的附加元数据，供具体实现传递协议/驱动额外参数。
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// IO 点解析器接口。
/// 将字符串格式的点标识解析为 <see cref="IOPointResolution"/>。
/// </summary>
public interface IIOPointResolver
{
    /// <summary>
    /// 解析 IO 点标识字符串。
    /// </summary>
    /// <param name="pointCode">IO 点标识字符串。</param>
    /// <returns>解析后的 IO 点信息。</returns>
    IOPointResolution Resolve(string pointCode);
}
