using System.Text.Json.Serialization;

namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Describes a single I/O action (Set, Pulse, Read, ReadVerify, WaitFor, Reset)
/// targeting a specific I/O point on a PLC device. I/O actions appear within
/// <see cref="FlowNodeDefinition"/> as PreIOActions or PostIOActions.
/// </summary>
public sealed class IOActionDefinition
{
    /// <summary>
    /// Unique identifier for this I/O action.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Human-readable name for this action (e.g., "Open Valve A").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of I/O: "DI" (digital input), "DO" (digital output),
    /// "AI" (analog input), "AO" (analog output).
    /// </summary>
    public string IOType { get; set; } = "DO";

    /// <summary>
    /// The <see cref="IOMappingDefinition.Id"/> of the I/O map that
    /// contains the target point.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="IOPointDefinition.Code"/> of the target point
    /// within the referenced I/O map.
    /// </summary>
    public string PointCode { get; set; } = string.Empty;

    /// <summary>
    /// The type of I/O action to perform.
    /// </summary>
    public IOActionType ActionType { get; set; } = IOActionType.Set;

    /// <summary>
    /// The value to write or compare. For Set/Pulse this is the output value;
    /// for ReadVerify this is the expected value; for WaitFor this is the
    /// condition value. May be a literal or a binding.
    /// </summary>
    public ValueExpression? Value { get; set; }

    /// <summary>
    /// Settling time in milliseconds to wait after writing before the
    /// action is considered complete (default 0 = no settle wait).
    /// </summary>
    public int SettleTimeMs { get; set; }

    /// <summary>
    /// Timeout in milliseconds for ReadVerify and WaitFor operations.
    /// When exceeded, the action fails according to the execution policy.
    /// Default 0 means no timeout (wait indefinitely).
    /// </summary>
    public int TimeoutMs { get; set; }

    /// <summary>
    /// Returns true when this action reads or checks a value
    /// rather than writing one.
    /// </summary>
    [JsonIgnore]
    public bool IsReadAction => ActionType is IOActionType.Read
        or IOActionType.ReadVerify
        or IOActionType.WaitFor;
}
