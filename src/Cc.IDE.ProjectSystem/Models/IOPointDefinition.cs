using System.Text.Json.Serialization;

namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// A single I/O point definition within an <see cref="IOGroupDefinition"/>.
/// Each point maps a physical PLC register address (or bit within a register)
/// to a logical name, data type, access mode, scaling configuration,
/// safe value, and polarity.
/// </summary>
public sealed class IOPointDefinition
{
    /// <summary>
    /// When false, this point is skipped during I/O operations.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Unique point identifier within the owning group
    /// (e.g., "DI_01", "AO_Temp_Setpoint").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable alias for this point (e.g., "Valve A Open Feedback").
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The data type: "bool", "byte", "short", "ushort", "int", "uint",
    /// "float", "double", "string".
    /// </summary>
    public string Type { get; set; } = "bool";

    /// <summary>
    /// Access mode: "Read", "Write", or "ReadWrite".
    /// </summary>
    public string Access { get; set; } = "ReadWrite";

    /// <summary>
    /// The Modbus data type for register interpretation:
    /// "Coil", "DiscreteInput", "HoldingRegister", "InputRegister",
    /// "InputByte", "OutputByte".
    /// </summary>
    public string DataType { get; set; } = "Coil";

    /// <summary>
    /// Polarity applied to the raw value before scaling.
    /// </summary>
    public IoPointPolarity Polarity { get; set; } = IoPointPolarity.Normal;

    /// <summary>
    /// Register offset relative to the parent group's <see cref="IOGroupDefinition.Offset"/>.
    /// </summary>
    public int RegisterOffset { get; set; }

    /// <summary>
    /// For bit-level types (Coil, DiscreteInput), the bit index within
    /// the 16-bit register (0-15). Ignored for register-level types.
    /// </summary>
    public int BitIndex { get; set; }

    /// <summary>
    /// Analog scaling definition. Null for digital points; required for
    /// analog points (AI/AO) to map raw values to engineering units.
    /// </summary>
    public AnalogScaleDefinition? Scale { get; set; }

    /// <summary>
    /// The safe/default value written to this point when a safety
    /// condition is triggered (e.g., emergency stop).
    /// </summary>
    public object? SafeValue { get; set; }

    /// <summary>
    /// The engineering unit label (e.g., "V", "mA", "degC", "bar", "%").
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-readable description of this point.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    // ─── Computed helpers ──────────────────────────────────────────

    /// <summary>
    /// Returns true when this point supports reading.
    /// </summary>
    [JsonIgnore]
    public bool CanRead => Access is "Read" or "ReadWrite";

    /// <summary>
    /// Returns true when this point supports writing.
    /// </summary>
    [JsonIgnore]
    public bool CanWrite => Access is "Write" or "ReadWrite";

    /// <summary>
    /// Returns true when this point uses analog scaling.
    /// </summary>
    [JsonIgnore]
    public bool IsAnalog => Type is "short" or "ushort" or "int"
        or "uint" or "float" or "double";

    /// <summary>
    /// Returns true when this point is a bit-level type (coil or discrete input).
    /// </summary>
    [JsonIgnore]
    public bool IsBitLevel => DataType is "Coil" or "DiscreteInput";

    /// <summary>
    /// Computes the absolute register address: group offset + point offset.
    /// </summary>
    [JsonIgnore]
    public int AbsoluteRegisterOffset => RegisterOffset; // Group offset is added at the group level during execution
}
