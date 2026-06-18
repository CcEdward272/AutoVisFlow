namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// The core unit of execution in the IDE. A task can operate in FlowGraph mode
/// (visual node-based flow) or StepTable mode (sequential step list).
/// It defines input/output arguments, local variables, nodes, links, layout info,
/// and an execution policy.
/// </summary>
public sealed class TaskDefinition
{
    /// <summary>
    /// Schema version for forward compatibility. Always "2.0" for this model.
    /// </summary>
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// Unique identifier for this task instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Human-readable name of the task.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this task does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Execution mode: "FlowGraph" for visual node-based flow,
    /// "StepTable" for sequential step-list execution.
    /// </summary>
    public string Mode { get; set; } = "FlowGraph";

    /// <summary>
    /// Arguments the task accepts from its caller.
    /// </summary>
    public List<TaskArgumentDefinition> InputArguments { get; set; } = new();

    /// <summary>
    /// Arguments the task returns to its caller.
    /// </summary>
    public List<TaskArgumentDefinition> OutputArguments { get; set; } = new();

    /// <summary>
    /// Task-scoped variables visible only within this task.
    /// </summary>
    public List<VariableDefinition> Variables { get; set; } = new();

    /// <summary>
    /// The nodes that make up this task's flow graph.
    /// </summary>
    public List<FlowNodeDefinition> Nodes { get; set; } = new();

    /// <summary>
    /// The directed links connecting nodes in the flow graph.
    /// </summary>
    public List<FlowLinkDefinition> Links { get; set; } = new();

    /// <summary>
    /// The saved pan/zoom state of the flow graph editor viewport.
    /// Null when the layout has not yet been saved.
    /// </summary>
    public FlowLayoutInfo? Layout { get; set; }

    /// <summary>
    /// Governs how this task responds to step failures and exceptions.
    /// Null means the system defaults apply.
    /// </summary>
    public TaskExecutionPolicy? ExecutionPolicy { get; set; }
}

/// <summary>
/// Describes a named, typed argument for a task's input or output signature.
/// </summary>
public sealed class TaskArgumentDefinition
{
    /// <summary>
    /// The argument name used in bindings and expressions.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The data type: "string", "bool", "int", or "double".
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// The default value used when the caller does not supply a binding.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// When true, the caller must supply a value for this argument.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Optional human-readable description of the argument.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
