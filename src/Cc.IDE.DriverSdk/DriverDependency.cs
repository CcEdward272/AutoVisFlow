namespace Cc.IDE.DriverSdk;

/// <summary>
/// Declares a dependency that an instrument driver has on another driver or external component.
/// Used by the IDE for dependency resolution and loading order.
/// </summary>
public sealed class DriverDependency
{
    /// <summary>
    /// Name or identifier of the dependency (e.g. driver ID or library name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Minimum version string required (e.g. "1.3.0"). Follows SemVer conventions.
    /// </summary>
    public string MinVersion { get; set; } = string.Empty;

    /// <summary>
    /// <c>true</c> if this dependency is required for the driver to function;
    /// <c>false</c> if the dependency is optional and the driver can degrade gracefully.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Human-readable description of why this dependency is needed.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
