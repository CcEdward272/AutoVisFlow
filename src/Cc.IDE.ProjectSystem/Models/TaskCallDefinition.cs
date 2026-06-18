namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Represents a call to a child task from within a parent task's flow graph.
/// Inputs supply values to the child's input arguments; Outputs capture the
/// child's return values into variables in the parent's scope.
/// </summary>
public sealed class TaskCallDefinition
{
    /// <summary>
    /// The unique ID of the task to invoke. Matches a <see cref="TaskDefinition.Id"/>.
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable or hierarchical path identifying the task
    /// (e.g., "ProjectA/SubTasks/Calibration").
    /// </summary>
    public string TaskPath { get; set; } = string.Empty;

    /// <summary>
    /// Values bound to the child task's input arguments, keyed by argument name.
    /// Each value may be a literal or a binding.
    /// </summary>
    public Dictionary<string, ValueExpression> Inputs { get; set; } = new();

    /// <summary>
    /// Targets where the child task's output arguments will be written.
    /// Keyed by the child task's output argument name.
    /// </summary>
    public Dictionary<string, BindingTargetDefinition> Outputs { get; set; } = new();
}

/// <summary>
/// Specifies where a task output value should be stored in the calling context.
/// </summary>
public sealed class BindingTargetDefinition
{
    /// <summary>
    /// The category of the target receiving the value.
    /// </summary>
    public BindingSource Target { get; set; } = BindingSource.TaskVariable;

    /// <summary>
    /// The identifier of the target instance (e.g., the variable name or device ID).
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// An optional sub-path within the target (e.g., a property of a structured variable).
    /// </summary>
    public string Path { get; set; } = string.Empty;
}
