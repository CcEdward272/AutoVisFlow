namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Defines one branch of a conditional (if/else-if/switch) construct
/// within a flow graph. Each branch specifies a boolean expression and
/// the node to jump to when the expression evaluates to true.
/// </summary>
public sealed class ConditionBranchDefinition
{
    /// <summary>
    /// A human-readable label shown on the branch in the flow graph editor
    /// (e.g., "Pass", "Fail", "Retry").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// A boolean expression evaluated at runtime (e.g., "x >= 5",
    /// "result == 'OK'").
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="FlowNodeDefinition.Id"/> of the node to navigate to
    /// when this branch's expression evaluates to true.
    /// </summary>
    public string TargetNodeId { get; set; } = string.Empty;
}
