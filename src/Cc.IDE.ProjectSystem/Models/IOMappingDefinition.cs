namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Defines a logical I/O mapping that binds a PLC device's physical address
/// space to named points and groups. An I/O map is referenced by projects
/// and used by <see cref="IOActionDefinition"/> to read and write points.
/// </summary>
public sealed class IOMappingDefinition
{
    /// <summary>
    /// Schema version for forward compatibility. Always "2.0" for this model.
    /// </summary>
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// Unique identifier for this I/O mapping instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Human-readable name of the I/O mapping
    /// (e.g., "Main PLC", "Remote IO Station 3").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of I/O: "DI", "DO", "AI", "AO", or "Mixed" for a map
    /// that contains multiple point types.
    /// </summary>
    public string IOType { get; set; } = "Mixed";

    /// <summary>
    /// The device identifier this mapping is associated with
    /// (e.g., a PLC name or slot reference).
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// The communication connection parameters for reaching this device.
    /// </summary>
    public IOConnectionConfig Connection { get; set; } = new();

    /// <summary>
    /// Logical groupings of I/O points (e.g., by register block, module, or
    /// functional area).
    /// </summary>
    public List<IOGroupDefinition> Groups { get; set; } = new();

    /// <summary>
    /// Optional human-readable description of this I/O mapping.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
