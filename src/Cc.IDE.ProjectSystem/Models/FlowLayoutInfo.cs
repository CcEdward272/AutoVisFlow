namespace Cc.IDE.ProjectSystem.Models;

/// <summary>
/// Stores the visual pan/zoom state of a flow graph editor viewport.
/// Persisted alongside the task definition so the editor restores the
/// user's last view.
/// </summary>
public sealed class FlowLayoutInfo
{
    /// <summary>
    /// Horizontal pan offset in canvas pixels.
    /// </summary>
    public double OffsetX { get; set; }

    /// <summary>
    /// Vertical pan offset in canvas pixels.
    /// </summary>
    public double OffsetY { get; set; }

    /// <summary>
    /// Zoom level where 1.0 is 100%. Values greater than 1 zoom in,
    /// values less than 1 zoom out.
    /// </summary>
    public double Zoom { get; set; } = 1.0;
}
