using System.Text.Json.Serialization;

namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Represents a single call to an instrument function within a flow node.
/// Multiple instrument calls on the same node are ordered by
/// <see cref="ExecutionOrder"/> and may declare dependencies via
/// <see cref="DependsOn"/> for parallel scheduling.
/// </summary>
public sealed class InstrumentCallDefinition
{
    /// <summary>
    /// Unique identifier for this call instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// The <see cref="InstrumentDefinition.Id"/> of the instrument to invoke.
    /// </summary>
    public string InstrumentId { get; set; } = string.Empty;

    /// <summary>
    /// The name or identifier of the function to call on the instrument
    /// (e.g., "Measure", "Configure", "Reset").
    /// </summary>
    public string FunctionId { get; set; } = string.Empty;

    /// <summary>
    /// Named parameters passed to the instrument function. Each value
    /// may be a literal or a binding.
    /// </summary>
    public Dictionary<string, ValueExpression> Parameters { get; set; } = new();

    /// <summary>
    /// When true, a failure in this call triggers the failure policy
    /// (stop, continue, retry). When false, failures are logged but ignored.
    /// </summary>
    public bool IsCritical { get; set; } = true;

    /// <summary>
    /// The execution position of this call relative to other calls on
    /// the same node. Lower numbers execute first.
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    /// Zero or more <see cref="InstrumentCallDefinition.Id"/> values of other
    /// calls on the same node that must complete before this call begins.
    /// Enables simple DAG-based parallel scheduling.
    /// </summary>
    public List<string> DependsOn { get; set; } = new();
}
