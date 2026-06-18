namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Represents a directed edge in a task's flow graph connecting two nodes.
/// Links encode control flow and may carry a label and optional guard condition.
/// </summary>
public sealed class FlowLinkDefinition
{
    /// <summary>
    /// The <see cref="FlowNodeDefinition.Id"/> of the source node.
    /// </summary>
    public string FromNodeId { get; set; } = string.Empty;

    /// <summary>
    /// The output port on the source node (e.g., "out", "success", "failure").
    /// "out" is the standard single-output port.
    /// </summary>
    public string FromPort { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="FlowNodeDefinition.Id"/> of the target node.
    /// </summary>
    public string ToNodeId { get; set; } = string.Empty;

    /// <summary>
    /// The input port on the target node (typically "in").
    /// </summary>
    public string ToPort { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-readable label displayed on the link in the editor.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// An optional guard expression. When non-empty, the link is only
    /// traversed when this boolean expression evaluates to true.
    /// </summary>
    public string Condition { get; set; } = string.Empty;
}
