using System.Text.Json.Serialization;

namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Represents a value that can be either a literal (locally-defined) or
/// a binding to an external data source. This dual nature allows every
/// parameter throughout the system to accept either a static value or a
/// dynamic runtime reference without requiring separate field types.
/// </summary>
public sealed class ValueExpression
{
    /// <summary>
    /// The literal value when no binding is used.
    /// </summary>
    public object? LocalValue { get; set; }

    /// <summary>
    /// The binding reference when the value is sourced from an external origin.
    /// </summary>
    public BindingDefinition? Binding { get; set; }

    /// <summary>
    /// Returns true when this expression is a binding rather than a local value.
    /// </summary>
    [JsonIgnore]
    public bool UsesBinding => Binding is not null;

    /// <summary>
    /// Creates a ValueExpression carrying a literal value.
    /// </summary>
    public static ValueExpression FromValue(object? value) => new() { LocalValue = value };

    /// <summary>
    /// Creates a ValueExpression carrying a binding reference.
    /// </summary>
    public static ValueExpression FromBinding(BindingDefinition binding) => new() { Binding = binding };
}
