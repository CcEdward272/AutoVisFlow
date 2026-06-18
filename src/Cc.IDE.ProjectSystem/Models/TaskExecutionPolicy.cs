namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Governs how a task responds to step-level and exception-level failures.
/// Attached to a <see cref="TaskDefinition"/> and may be overridden at
/// the step level.
/// </summary>
public sealed class TaskExecutionPolicy
{
    /// <summary>
    /// The policy applied when a step reports a failure (e.g., assertion failure).
    /// </summary>
    public FailurePolicy FailurePolicy { get; set; } = FailurePolicy.StopOnFailure;

    /// <summary>
    /// The policy applied when a step throws an unhandled exception.
    /// </summary>
    public FailurePolicy ExceptionPolicy { get; set; } = FailurePolicy.StopOnFailure;

    /// <summary>
    /// When <see cref="FailurePolicy"/> or <see cref="ExceptionPolicy"/> is
    /// RetryThenStop or RetryThenContinue, this specifies the maximum number
    /// of retry attempts before applying the final action.
    /// </summary>
    public int GlobalRetryCount { get; set; }
}
