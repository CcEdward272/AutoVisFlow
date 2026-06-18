namespace Cc.IDE.DriverSdk;

/// <summary>
/// 描述 <see cref="InstrumentCapability"/> 的单个输入参数。
/// IDE 使用此元数据在 UI 中自动生成属性编辑器。
/// </summary>
public sealed class CapabilityParameter
{
    /// <summary>
    /// 参数的程序化名称（如 "range"、"channel"）。
    /// 作为参数键名传入 <see cref="IInstrumentDriver.ExecuteAsync"/> 的 <c>parameters</c> 字典。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 在 UI 中显示的人类可读标签（如 "电压量程"）。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 参数的数据类型："string"、"int"、"double"、"bool"、"enum"、"string[]" 等。
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// 物理单位标签（如适用），例如 "V"、"A"、"Hz"、"s"。
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 用户未提供此参数时使用的默认值。
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// 若此参数为必填则为 <c>true</c>；可选则为 <c>false</c>。
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 指示 UI 中应使用哪种编辑器控件的提示
    /// （如 "text"、"number"、"dropdown"、"checkbox"、"file-picker"）。
    /// </summary>
    public string? EditorHint { get; set; }

    /// <summary>
    /// 枚举或下拉参数的允许值列表。为 <c>null</c> 表示接受任意值。
    /// </summary>
    public IReadOnlyList<object>? AllowedValues { get; set; }

    /// <summary>
    /// 数值参数的最小允许值。
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// 数值参数的最大允许值。
    /// </summary>
    public double? MaxValue { get; set; }

    /// <summary>
    /// 描述此参数用途的帮助文本。
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
