namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Defines a physical or virtual instrument accessible to the test system.
/// Instruments are referenced by projects and invoked at runtime via
/// <see cref="InstrumentCallDefinition"/> instances within flow nodes.
/// </summary>
public sealed class InstrumentDefinition
{
    /// <summary>
    /// Schema version for forward compatibility. Always "2.0" for this model.
    /// </summary>
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// Unique identifier for this instrument instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Human-readable name of the instrument (e.g., "DMM", "Power Supply A").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The driver ID that provides the communication protocol
    /// and command set for this instrument.
    /// </summary>
    public string DriverId { get; set; } = string.Empty;

    /// <summary>
    /// The type or model of the device (e.g., "Agilent34401A", "NI-DAQmx").
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// Whether this instrument is enabled for use. Disabled instruments
    /// are skipped during execution.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The physical or virtual connection parameters for this instrument.
    /// </summary>
    public InstrumentConnection Connection { get; set; } = new();

    /// <summary>
    /// Calibration metadata and compensation settings for this instrument.
    /// Arbitrary key-value pairs defined by the driver.
    /// </summary>
    public Dictionary<string, object?> CalibrationSettings { get; set; } = new();

    /// <summary>
    /// User-defined preferences for this instrument instance
    /// (e.g., display units, default ranges).
    /// </summary>
    public Dictionary<string, object?> Preferences { get; set; } = new();

    /// <summary>
    /// Optional human-readable description of the instrument and its purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
