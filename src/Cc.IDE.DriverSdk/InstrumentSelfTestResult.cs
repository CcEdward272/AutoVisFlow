namespace Cc.IDE.DriverSdk;

/// <summary>
/// Result of an instrument self-test operation.
/// </summary>
public sealed class InstrumentSelfTestResult
{
    /// <summary>
    /// <c>true</c> if all self-test checks passed; otherwise <c>false</c>.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Summary message describing the overall test outcome.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed per-check diagnostic messages.
    /// </summary>
    public List<string> Details { get; set; } = new();

    /// <summary>
    /// Total elapsed time for the self-test in milliseconds.
    /// </summary>
    public long ElapsedMs { get; set; }
}
