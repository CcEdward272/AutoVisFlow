namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// A named group of I/O points within an <see cref="IOMappingDefinition"/>.
/// Groups organize points by register kind and offset, making it easier
/// to manage large device maps (e.g., one group per module or function block).
/// </summary>
public sealed class IOGroupDefinition
{
    /// <summary>
    /// Unique identifier for this group within the owning I/O map.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Human-readable name for this group
    /// (e.g., "Digital Inputs Slot 1", "Analog Outputs").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When false, all points in this group are skipped during execution.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The PLC register kind: "HoldingRegister", "InputRegister",
    /// "Coil", "DiscreteInput", "InputByte", "OutputByte".
    /// </summary>
    public string RegisterKind { get; set; } = "HoldingRegister";

    /// <summary>
    /// The base register offset for this group. Individual point offsets
    /// are added to this base to compute the absolute register address.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// The ordered list of I/O points in this group.
    /// </summary>
    public List<IOPointDefinition> Points { get; set; } = new();
}
