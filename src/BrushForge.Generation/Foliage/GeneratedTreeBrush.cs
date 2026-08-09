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
        Bounds3d bounds,
        string? branchPath = null,
        int branchSegmentIndex = -1,
        int branchSegmentCount = 0)
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

        if (role == TreeBrushRole.Branch) {
            ArgumentException.ThrowIfNullOrWhiteSpace(branchPath);

            if (branchSegmentCount <= 0) {
                throw new ArgumentOutOfRangeException(
                    nameof(branchSegmentCount),
                    branchSegmentCount,
                    "A branch brush requires a positive branch segment count.");
            }

            if (
                branchSegmentIndex < 0 ||
                branchSegmentIndex >= branchSegmentCount
            ) {
                throw new ArgumentOutOfRangeException(
                    nameof(branchSegmentIndex),
                    branchSegmentIndex,
                    "A branch segment index must be within its branch segment count.");
            }
        }
        else {
            if (branchPath is not null) {
                throw new ArgumentException(
                    "Only branch brushes may carry a branch path.",
                    nameof(branchPath));
            }

            if (
                branchSegmentIndex != -1 ||
                branchSegmentCount != 0
            ) {
                throw new ArgumentException(
                    "Only branch brushes may carry branch segment metadata.",
                    nameof(branchSegmentIndex));
            }
        }

        Role = role;
        CanopyLayerIndex = canopyLayerIndex;
        Brush = brush;
        Bounds = bounds;
        BranchPath = branchPath?.Trim();
        BranchSegmentIndex = branchSegmentIndex;
        BranchSegmentCount = branchSegmentCount;
    }

    public TreeBrushRole Role { get; }

    public int CanopyLayerIndex { get; }

    public ConvexBrush Brush { get; }

    public Bounds3d Bounds { get; }

    /// <summary>
    /// Stable skeleton path for branch brushes; null for trunk and canopy.
    /// </summary>
    public string? BranchPath { get; }

    /// <summary>
    /// Zero-based segment position within a realized branch path; -1 for
    /// trunk and canopy brushes.
    /// </summary>
    public int BranchSegmentIndex { get; }

    /// <summary>
    /// Number of connected brush segments currently realizing the branch;
    /// zero for trunk and canopy brushes.
    /// </summary>
    public int BranchSegmentCount { get; }
}
