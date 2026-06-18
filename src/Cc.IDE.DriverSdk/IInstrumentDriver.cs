using Cc.IDE.Communication;

namespace Cc.IDE.DriverSdk;

/// <summary>
/// 每个仪器驱动必须实现的主要接口。
/// 提供生命周期管理（连接/断开）、能力发现、命令执行和特定仪器型号的健康监控。
/// </summary>
/// <remarks>
/// 实现应保持无状态（除传输连接之外），以便多个测试序列可以共享同一个驱动实例。
/// </remarks>
public interface IInstrumentDriver : IDisposable
{
    // ── 标识信息 ──────────────────────────────────────────────────────

    /// <summary>
    /// 驱动全局唯一标识符（如 "Keysight.34465A"）。用于驱动查找和依赖解析。
    /// </summary>
    string DriverId { get; }

    /// <summary>
    /// 人类可读的显示名称（如 "Keysight 34465A Digital Multimeter"）。
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// 驱动版本号，遵循 SemVer 规范（如 "1.0.0"）。
    /// </summary>
    string Version { get; }

    /// <summary>
    /// 驱动控制的设备类型（如 "DMM"、"PowerSupply"、"Oscilloscope"）。
    /// </summary>
    string DeviceType { get; }

    /// <summary>
    /// 制造商名称（如 "Keysight"、"Tektronix"、"Fluke"）。
    /// </summary>
    string Manufacturer { get; }

    /// <summary>
    /// 驱动支持的传输协议列表（如 ["Serial", "TCP", "GPIB"]）。
    /// </summary>
    IReadOnlyList<string> SupportedTransports { get; }

    /// <summary>
    /// 驱动声明的对其他驱动或外部组件的依赖列表。
    /// </summary>
    IReadOnlyList<DriverDependency> Dependencies { get; }

    // ── 连接状态 ──────────────────────────────────────────────────────

    /// <summary>
    /// 当驱动当前已连接至仪器时为 <c>true</c>。
    /// </summary>
    bool IsConnected { get; }

    // ── 生命周期 ─────────────────────────────────────────────────────

    /// <summary>
    /// 使用给定的传输配置建立与仪器的连接。
    /// </summary>
    /// <param name="connection">连接参数（协议、地址、串口设置等）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <exception cref="InvalidOperationException">已处于连接状态时抛出。</exception>
    /// <exception cref="TimeoutException">连接超时时抛出。</exception>
    Task ConnectAsync(DriverConnectionData connection, CancellationToken ct);

    /// <summary>
    /// 优雅断开与仪器的连接并释放传输资源。已断开时调用不会产生副作用。
    /// </summary>
    Task DisconnectAsync();

    // ── 能力发现 ──────────────────────────────────────────────────────

    /// <summary>
    /// 返回此仪器支持的完整能力（功能）列表。
    /// </summary>
    /// <returns>包含参数和输出定义的仪器能力只读列表。</returns>
    IReadOnlyList<InstrumentCapability> GetCapabilities();

    // ── 命令执行 ──────────────────────────────────────────────────────

    /// <summary>
    /// 在仪器上执行指定的命名能力。
    /// </summary>
    /// <param name="functionId">待执行的 <see cref="InstrumentCapability.FunctionId"/>。</param>
    /// <param name="parameters">命名的能力参数值。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>
    /// 包含成功/失败状态、输出和计时信息的 <see cref="InstrumentResult"/>。
    /// </returns>
    /// <exception cref="InvalidOperationException">未连接时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="functionId"/> 未知时抛出。</exception>
    Task<InstrumentResult> ExecuteAsync(
        string functionId,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct);

    // ── 健康状态与诊断 ────────────────────────────────────────────────

    /// <summary>
    /// 返回仪器连接的当前健康状态。
    /// </summary>
    /// <returns><see cref="InstrumentHealth"/> 状态快照。</returns>
    Task<InstrumentHealth> GetHealthAsync();

    /// <summary>
    /// 查询仪器的识别字符串（如 "*IDN?" 响应）。
    /// </summary>
    /// <returns>仪器的识别字符串。</returns>
    /// <exception cref="InvalidOperationException">未连接时抛出。</exception>
    Task<string> GetIdentificationAsync();

    /// <summary>
    /// 将仪器重置为已知的默认状态。
    /// </summary>
    /// <exception cref="InvalidOperationException">未连接时抛出。</exception>
    Task ResetAsync();

    /// <summary>
    /// 运行仪器内置自检。
    /// </summary>
    /// <returns>包含通过/失败状态和详细信息的 <see cref="InstrumentSelfTestResult"/>。</returns>
    Task<InstrumentSelfTestResult> SelfTestAsync();
}
