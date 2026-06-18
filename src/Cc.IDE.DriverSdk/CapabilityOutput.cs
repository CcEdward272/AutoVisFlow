namespace Cc.IDE.DriverSdk;

/// <summary>
/// 描述 <see cref="InstrumentCapability"/> 产生的单个输出值。
/// 输出值通过 <see cref="InstrumentResult.Outputs"/> 字典返回。
/// </summary>
public sealed class CapabilityOutput
{
    /// <summary>
    /// 输出的程序化名称（如 "voltage"、"frequency"）。
    /// 作为 <see cref="InstrumentResult.Outputs"/> 字典中的键名使用。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 在 UI 中显示的人类可读标签（如 "测量电压"）。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 输出的数据类型："string"、"int"、"double"、"bool"、"double[]" 等。
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// 物理单位标签（如适用），例如 "V"、"A"、"Hz"。
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 描述此输出含义的帮助文本。
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
