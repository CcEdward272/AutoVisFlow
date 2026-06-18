namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// The top-level container in the project hierarchy.
/// A solution groups one or more projects, instruments, I/O maps, and drivers
/// into a single test-automation workspace. It also holds globally-scoped
/// variables and may designate a startup project.
/// </summary>
public sealed class SolutionDefinition
{
    /// <summary>
    /// Schema version for forward compatibility. Always "2.0" for this model.
    /// </summary>
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// Unique identifier for this solution instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Human-readable name of the solution.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the solution's purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ISO 8601 timestamp of the solution's creation.
    /// </summary>
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    /// <summary>
    /// ISO 8601 timestamp of the last modification.
    /// </summary>
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");

    /// <summary>
    /// Ordered list of project IDs that belong to this solution.
    /// </summary>
    public List<string> Projects { get; set; } = new();

    /// <summary>
    /// Ordered list of instrument reference IDs used by this solution.
    /// </summary>
    public List<string> InstrumentRefs { get; set; } = new();

    /// <summary>
    /// Ordered list of I/O map reference IDs used by this solution.
    /// </summary>
    public List<string> IOMapRefs { get; set; } = new();

    /// <summary>
    /// Ordered list of driver reference IDs required by this solution.
    /// </summary>
    public List<string> DriverRefs { get; set; } = new();

    /// <summary>
    /// Global key-value settings shared across all projects.
    /// </summary>
    public Dictionary<string, object?> Globals { get; set; } = new();

    /// <summary>
    /// Optional project ID that is the default startup target when the solution runs.
    /// When null, the user must select a project at runtime.
    /// </summary>
    public string? StartupProject { get; set; }
}
