namespace Cc.IDE.DriverSdk;

/// <summary>
/// 描述仪器驱动可执行的离散能力（功能）。
/// 每个能力对应一个 <c>functionId</c>，用于 <see cref="IInstrumentDriver.ExecuteAsync"/>。
/// </summary>
public sealed class InstrumentCapability
{
    /// <summary>
    /// 此能力在驱动内的唯一标识符（如 "MeasureVoltageDC"）。
    /// 作为 <c>functionId</c> 参数传入 <see cref="IInstrumentDriver.ExecuteAsync"/>。
    /// </summary>
    public string FunctionId { get; set; } = string.Empty;

    /// <summary>
    /// 在 UI 中显示的人类可读名称（如 "直流电压测量"）。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 能力的分类分组，用于菜单或树形列表中的组织（如 "测量"、"配置"、"输出"）。
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 此能力功能的详细描述文本。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 若此能力可在自动化测试序列中作为步骤使用则为 <c>true</c>。
    /// </summary>
    public bool IsTestStepCapable { get; set; }

    /// <summary>
    /// 此操作预计的典型耗时（毫秒）。用于测试计划时间估算和超时默认值。
    /// </summary>
    public long EstimatedDurationMs { get; set; }

    /// <summary>
    /// 此能力接受的输入参数定义列表。
    /// </summary>
    public IReadOnlyList<CapabilityParameter> Parameters { get; set; } = Array.Empty<CapabilityParameter>();

    /// <summary>
    /// 此能力产生的输出定义列表。
    /// </summary>
    public IReadOnlyList<CapabilityOutput> Outputs { get; set; } = Array.Empty<CapabilityOutput>();
}
