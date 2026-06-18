namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Describes a binding from an external data source to a runtime value.
/// Used by <see cref="ValueExpression"/> to resolve dynamic values at
/// execution time rather than hard-coding literals.
/// </summary>
public sealed class BindingDefinition
{
    /// <summary>
    /// The category of source providing the bound value.
    /// </summary>
    public BindingSource Source { get; set; } = BindingSource.Unknown;

    /// <summary>
    /// The identifier of the specific source instance (e.g., a task ID,
    /// project ID, device ID, or variable name depending on Source).
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// An optional sub-path or property path within the source (e.g.,
    /// "Result.Voltage" for a structured output).
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// An optional format string applied when converting the bound value
    /// to a string (e.g., "F2" for two decimal places).
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// An optional converter key used to transform the bound value
    /// (e.g., "IntToBool", "ScalePercent").
    /// </summary>
    public string Converter { get; set; } = string.Empty;
}
