using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Geometry;

namespace BrushForge.Generation.Foliage;

/// <summary>
/// Builds a low-brush-count trunk from contiguous convex frustum segments.
/// </summary>
internal static class TaperedTrunkGenerator
{
    public static IReadOnlyList<GeneratedTreeBrush> Generate(
        Vector3d origin,
        int trunkWidthUnits,
        int trunkTopUnits,
        int requestedSegmentCount,
        double taper,
        double grid,
        string textureName)
    {
        if (trunkWidthUnits < 1) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkWidthUnits),
                trunkWidthUnits,
                "The trunk width must contain at least one grid unit.");
        }
        if (trunkTopUnits < 1) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkTopUnits),
                trunkTopUnits,
                "The trunk height must contain at least one grid unit.");
        }

        if (
            requestedSegmentCount <
                TreeGenerationSettings.MinimumTrunkSegmentCount ||
            requestedSegmentCount >
                TreeGenerationSettings.MaximumTrunkSegmentCount
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(requestedSegmentCount),
                requestedSegmentCount,
                $"The requested trunk segment count must be between {TreeGenerationSettings.MinimumTrunkSegmentCount} and {TreeGenerationSettings.MaximumTrunkSegmentCount}.");
        }

        if (
            !double.IsFinite(taper) ||
            taper < TreeGenerationSettings.MinimumTrunkTaper ||
            taper > TreeGenerationSettings.MaximumTrunkTaper
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(taper),
                taper,
                $"The trunk taper must be between {TreeGenerationSettings.MinimumTrunkTaper:P0} and {TreeGenerationSettings.MaximumTrunkTaper:P0}.");
        }
        if (!double.IsFinite(grid) || grid <= 0.0) {
            throw new ArgumentOutOfRangeException(
                nameof(grid),
                grid,
                "The trunk grid size must be finite and greater than zero.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(textureName);

        if (requestedSegmentCount > trunkTopUnits) {
            throw new ArgumentOutOfRangeException(
                nameof(requestedSegmentCount),
                requestedSegmentCount,
                "The requested trunk segment count cannot exceed the trunk height in grid units.");
        }

        int segmentCount =
            requestedSegmentCount;

        int topWidthUnits =
            Math.Clamp(
                (int)Math.Round(
                    trunkWidthUnits *
                    (1.0 - taper),
                    MidpointRounding.AwayFromZero),
                1,
                trunkWidthUnits);
        bool useOctagonalRings =
            topWidthUnits >= 3;
        int baseSegmentHeightUnits =
            trunkTopUnits / segmentCount;
        int remainingHeightUnits =
            trunkTopUnits % segmentCount;
        int currentBottomUnits = 0;
        List<GeneratedTreeBrush> parts = [];

        for (
            int segmentIndex = 0;
            segmentIndex < segmentCount;
            segmentIndex++
        ) {
            int segmentHeightUnits =
                baseSegmentHeightUnits +
                (
                    segmentIndex < remainingHeightUnits
                        ? 1
                        : 0
                );
            int currentTopUnits =
                currentBottomUnits +
                segmentHeightUnits;
            int bottomWidthUnits =
                InterpolateWidthUnits(
                    trunkWidthUnits,
                    topWidthUnits,
                    segmentIndex,
                    segmentCount);
            int segmentTopWidthUnits =
                InterpolateWidthUnits(
                    trunkWidthUnits,
                    topWidthUnits,
                    segmentIndex + 1,
                    segmentCount);
            double bottomZ =
                origin.Z +
                (currentBottomUnits * grid);
            double topZ =
                origin.Z +
                (currentTopUnits * grid);
            Vector3d[] bottomRing =
                CreateRing(
                    origin,
                    bottomWidthUnits,
                    bottomZ,
                    grid,
                    useOctagonalRings);
            Vector3d[] topRing =
                CreateRing(
                    origin,
                    segmentTopWidthUnits,
                    topZ,
                    grid,
                    useOctagonalRings);
            ConvexBrush brush =
                VerticalConvexFrustumBrushFactory.Create(
                    bottomRing,
                    topRing,
                    textureName);
            Bounds3d bounds =
                Bounds3d.FromPoints(
                    bottomRing.Concat(
                        topRing));

            parts.Add(
                new GeneratedTreeBrush(
                    TreeBrushRole.Trunk,
                    canopyLayerIndex: -1,
                    brush,
                    bounds));
            currentBottomUnits =
                currentTopUnits;
        }

        return parts;
    }

    private static int InterpolateWidthUnits(
        int baseWidthUnits,
        int topWidthUnits,
        int levelIndex,
        int segmentCount)
    {
        int totalReduction =
            baseWidthUnits -
            topWidthUnits;
        int appliedReduction =
            (
                (totalReduction * levelIndex) +
                (segmentCount / 2)
            ) /
            segmentCount;

        return baseWidthUnits -
            appliedReduction;
    }

    private static Vector3d[] CreateRing(
        Vector3d origin,
        int widthUnits,
        double z,
        double grid,
        bool useOctagonalRing)
    {
        // Widths can change from an even to an odd number of grid units.
        // Using integer offsets would shift odd-width rings by half a grid
        // unit, causing one side of the trunk to remain visually vertical.
        // Half-grid coordinates keep every ring centered on the same axis.
        double halfWidth =
            (widthUnits * grid) /
            2.0;
        double minimumX =
            origin.X -
            halfWidth;
        double minimumY =
            origin.Y -
            halfWidth;
        double maximumX =
            origin.X +
            halfWidth;
        double maximumY =
            origin.Y +
            halfWidth;

        if (!useOctagonalRing) {
            return
            [
                new Vector3d(
                    minimumX,
                    minimumY,
                    z),
                new Vector3d(
                    maximumX,
                    minimumY,
                    z),
                new Vector3d(
                    maximumX,
                    maximumY,
                    z),
                new Vector3d(
                    minimumX,
                    maximumY,
                    z)
            ];
        }

        double inset = grid;
        return
        [
            new Vector3d(
                minimumX + inset,
                minimumY,
                z),
            new Vector3d(
                maximumX - inset,
                minimumY,
                z),
            new Vector3d(
                maximumX,
                minimumY + inset,
                z),
            new Vector3d(
                maximumX,
                maximumY - inset,
                z),
            new Vector3d(
                maximumX - inset,
                maximumY,
                z),
            new Vector3d(
                minimumX + inset,
                maximumY,
                z),
            new Vector3d(
                minimumX,
                maximumY - inset,
                z),
            new Vector3d(
                minimumX,
                minimumY + inset,
                z)
        ];
    }
}
