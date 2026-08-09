using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;

namespace BrushForge.Generation.Foliage;

/// <summary>
/// One generated tree brush with preview and material-mapping metadata.
/// </summary>
public sealed record GeneratedTreeBrush
{
    public GeneratedTreeBrush(
        TreeBrushRole role,
        int canopyLayerIndex,
        ConvexBrush brush,
        Bounds3d bounds)
    {
        if (!Enum.IsDefined(role)) {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "The tree brush role is not recognized.");
        }

        ArgumentNullException.ThrowIfNull(brush);

        if (
            role != TreeBrushRole.Canopy &&
            canopyLayerIndex != -1
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(canopyLayerIndex),
                canopyLayerIndex,
                "A trunk or branch brush must use canopy layer index -1.");
        }

        if (
            role == TreeBrushRole.Canopy &&
            canopyLayerIndex < 0
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(canopyLayerIndex),
                canopyLayerIndex,
                "A canopy brush requires a non-negative layer index.");
        }

        Role = role;
        CanopyLayerIndex = canopyLayerIndex;
        Brush = brush;
        Bounds = bounds;
    }

    public TreeBrushRole Role { get; }

    public int CanopyLayerIndex { get; }

    public ConvexBrush Brush { get; }

    public Bounds3d Bounds { get; }
}
