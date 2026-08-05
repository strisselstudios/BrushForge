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
    private const int MinimumSegmentCount = 2;
    private const int MaximumSegmentCount = 4;
    private const int PreferredSegmentHeightUnits = 6;

    public static IReadOnlyList<GeneratedTreeBrush> Generate(
        Vector3d origin,
        int trunkWidthUnits,
        int trunkTopUnits,
        double grid,
        string textureName)
    {
        if (trunkWidthUnits < 1) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkWidthUnits),
                trunkWidthUnits,
                "The trunk width must contain at least one grid unit.");
        }
        if (trunkTopUnits < MinimumSegmentCount) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkTopUnits),
                trunkTopUnits,
                $"The trunk height must contain at least {MinimumSegmentCount} grid units.");
        }
        if (!double.IsFinite(grid) || grid <= 0.0) {
            throw new ArgumentOutOfRangeException(
                nameof(grid),
                grid,
                "The trunk grid size must be finite and greater than zero.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(textureName);

        int segmentCount =
            CalculateSegmentCount(
                trunkTopUnits);
        int topWidthUnits =
            Math.Max(
                1,
                trunkWidthUnits -
                (trunkWidthUnits >= 4 ? 1 : 0));
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

    private static int CalculateSegmentCount(
        int trunkTopUnits)
    {
        int preferredCount =
            Math.Clamp(
                (
                    trunkTopUnits +
                    PreferredSegmentHeightUnits -
                    1
                ) /
                PreferredSegmentHeightUnits,
                MinimumSegmentCount,
                MaximumSegmentCount);

        return Math.Min(
            preferredCount,
            trunkTopUnits);
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
        int minimumOffsetUnits =
            -(widthUnits / 2);
        int maximumOffsetUnits =
            minimumOffsetUnits +
            widthUnits;
        double minimumX =
            origin.X +
            (minimumOffsetUnits * grid);
        double minimumY =
            origin.Y +
            (minimumOffsetUnits * grid);
        double maximumX =
            origin.X +
            (maximumOffsetUnits * grid);
        double maximumY =
            origin.Y +
            (maximumOffsetUnits * grid);

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
