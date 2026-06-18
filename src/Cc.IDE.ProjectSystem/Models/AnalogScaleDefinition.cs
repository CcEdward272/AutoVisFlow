namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Defines the scaling relationship between raw hardware values (e.g., 0-4095)
/// and engineering units for an analog I/O point.
/// </summary>
public sealed class AnalogScaleDefinition
{
    /// <summary>
    /// The type of scaling curve to apply.
    /// </summary>
    public AnalogScaleType Type { get; set; } = AnalogScaleType.Linear;

    /// <summary>
    /// The minimum raw value from the hardware (typically 0 for 4-20 mA, 0-4095 ADC).
    /// </summary>
    public double RawMin { get; set; }

    /// <summary>
    /// The maximum raw value from the hardware (default 4095 for 12-bit ADC).
    /// </summary>
    public double RawMax { get; set; } = 4095;

    /// <summary>
    /// The engineering-unit value corresponding to RawMin.
    /// </summary>
    public double ScaledMin { get; set; }

    /// <summary>
    /// The engineering-unit value corresponding to RawMax (default 100 for percentage).
    /// </summary>
    public double ScaledMax { get; set; } = 100;

    /// <summary>
    /// Intermediate breakpoints used when Type is PiecewiseLinear.
    /// Null for linear scaling.
    /// </summary>
    public List<PiecewiseScalePoint>? PiecewisePoints { get; set; }
}

/// <summary>
/// A single breakpoint in a piecewise-linear analog scaling curve.
/// </summary>
public sealed class PiecewiseScalePoint
{
    /// <summary>
    /// The raw hardware value at this breakpoint.
    /// </summary>
    public double Raw { get; set; }

    /// <summary>
    /// The scaled engineering-unit value at this breakpoint.
    /// </summary>
    public double Scaled { get; set; }
}
