namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Defines the type of action to perform on an I/O point.
/// Set = write a value, Pulse = brief activation/deactivation,
/// Read = read current value, ReadVerify = read with comparison,
/// WaitFor = block until condition met, Reset = reset to default.
/// </summary>
public enum IOActionType
{
    Set,
    Pulse,
    Read,
    ReadVerify,
    WaitFor,
    Reset
}

/// <summary>
/// Defines the type of loop construct in a flow graph.
/// </summary>
public enum LoopType
{
    ForLoop,
    WhileLoop,
    ForEach
}

/// <summary>
/// Defines how task execution responds to a step failure.
/// </summary>
public enum FailurePolicy
{
    StopOnFailure,
    ContinueOnFailure,
    RetryThenStop,
    RetryThenContinue
}

/// <summary>
/// Identifies the origin of a bound value within the runtime system.
/// </summary>
public enum BindingSource
{
    Unknown,
    TaskInput,
    TaskOutput,
    TaskVariable,
    ProjectVariable,
    GlobalVariable,
    ToolOutput,
    DevicePoint,
    RuntimeContext
}

/// <summary>
/// Polarity applied to a physical I/O point.
/// Normal = raw value used as-is, Inverted = logical inversion.
/// </summary>
public enum IoPointPolarity
{
    Normal,
    Inverted
}

/// <summary>
/// Specifies how analog raw values map to scaled engineering values.
/// Linear applies a two-point line; PiecewiseLinear uses intermediate breakpoints.
/// </summary>
public enum AnalogScaleType
{
    Linear,
    PiecewiseLinear
}
