namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Represents a project within a <see cref="SolutionDefinition"/>.
/// A project is a collection of tasks, instruments, I/O maps, and templates
/// that together define a test sequence or automation workflow.
/// </summary>
public sealed class ProjectDefinition
{
    /// <summary>
    /// Schema version for forward compatibility. Always "2.0" for this model.
    /// </summary>
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// Unique identifier for this project instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Human-readable name of the project.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the project's purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The task ID of the entry-point task executed when the project starts.
    /// Null means no task will auto-start.
    /// </summary>
    public string? EntryTask { get; set; }

    /// <summary>
    /// Ordered list of task IDs belonging to this project, defining
    /// the display and execution order.
    /// </summary>
    public List<string> Tasks { get; set; } = new();

    /// <summary>
    /// Ordered list of instrument IDs associated with this project.
    /// </summary>
    public List<string> InstrumentIds { get; set; } = new();

    /// <summary>
    /// Ordered list of I/O map IDs associated with this project.
    /// </summary>
    public List<string> IOMapIds { get; set; } = new();

    /// <summary>
    /// Ordered list of template references (template IDs) used by this project.
    /// </summary>
    public List<string> TemplateRefs { get; set; } = new();

    /// <summary>
    /// Project-scoped variables visible to all tasks in this project.
    /// </summary>
    public List<VariableDefinition> Variables { get; set; } = new();

    /// <summary>
    /// Arbitrary project-level setting key-value pairs.
    /// </summary>
    public Dictionary<string, object?> Settings { get; set; } = new();
}
