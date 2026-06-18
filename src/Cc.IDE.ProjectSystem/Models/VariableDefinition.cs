namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Represents a typed variable declared at the Solution, Project, or Task scope.
/// Variables flow through the runtime context and can be bound as inputs/outputs
/// of tasks, instruments, and I/O actions.
/// </summary>
public sealed class VariableDefinition
{
    /// <summary>
    /// The unique name of this variable within its parent scope.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The data type: "string", "bool", "int", or "double".
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// The default value assigned when the variable is initialized.
    /// Must be compatible with <see cref="Type"/>.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// The visibility scope: "Solution", "Project", or "Task".
    /// </summary>
    public string Scope { get; set; } = "Task";

    /// <summary>
    /// Optional human-readable description of the variable's purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
