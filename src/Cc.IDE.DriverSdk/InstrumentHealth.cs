namespace Cc.IDE.DriverSdk;

/// <summary>
/// Represents the current health state of an instrument connection.
/// Used for monitoring and diagnostics.
/// </summary>
public sealed class InstrumentHealth
{
    /// <summary>
    /// <c>true</c> when the transport is currently connected to the instrument.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Overall status label, e.g. "Healthy", "Degraded", "Disconnected", "Error".
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// The most recent error message, if any.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Timestamp of the last successful communication with the instrument.
    /// </summary>
    public DateTime? LastSuccessfulCommunication { get; set; }

    /// <summary>
    /// Number of consecutive failed communication attempts since the last success.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Rolling average response time in milliseconds for recent operations.
    /// </summary>
    public double AverageResponseMs { get; set; }
}
