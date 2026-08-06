using BrushForge.Core.Randomness;
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
    private const ulong DeformationSeedSalt =
        0xD1B54A32D192ED03UL;

    private const ulong BaseFlareSeedSalt =
        0x94D049BB133111EBUL;

    public static GeneratedTrunk Generate(
        Vector3d origin,
        int trunkWidthUnits,
        int trunkTopUnits,
        int requestedSegmentCount,
        double taper,
        double lean,
        double bend,
        double baseFlare,
        GenerationSeed generationSeed,
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

        ValidateFraction(
            taper,
            TreeGenerationSettings.MinimumTrunkTaper,
            TreeGenerationSettings.MaximumTrunkTaper,
            nameof(taper),
            "trunk taper");
        ValidateFraction(
            lean,
            TreeGenerationSettings.MinimumTrunkLean,
            TreeGenerationSettings.MaximumTrunkLean,
            nameof(lean),
            "trunk lean");
        ValidateFraction(
            bend,
            TreeGenerationSettings.MinimumTrunkBend,
            TreeGenerationSettings.MaximumTrunkBend,
            nameof(bend),
            "trunk bend");
        ValidateFraction(
            baseFlare,
            TreeGenerationSettings.MinimumTrunkBaseFlare,
            TreeGenerationSettings.MaximumTrunkBaseFlare,
            nameof(baseFlare),
            "trunk base flare");

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
        int baseFlareAddedWidthUnits =
            CalculateBaseFlareAddedWidthUnits(
                trunkWidthUnits,
                baseFlare);
        int flaredBaseWidthUnits =
            trunkWidthUnits +
            baseFlareAddedWidthUnits;
        Vector3d flaredBaseCenter =
            CreateBaseFlareCenter(
                origin,
                baseFlareAddedWidthUnits,
                generationSeed,
                grid);
        bool useOctagonalRings =
            topWidthUnits >= 3;
        int baseSegmentHeightUnits =
            trunkTopUnits / segmentCount;
        int remainingHeightUnits =
            trunkTopUnits % segmentCount;
        Vector3d[] ringCenters =
            CreateRingCenters(
                origin,
                trunkTopUnits,
                segmentCount,
                lean,
                bend,
                generationSeed,
                grid);
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
                segmentIndex == 0
                    ? flaredBaseWidthUnits
                    : InterpolateWidthUnits(
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
            Vector3d bottomRingCenter =
                segmentIndex == 0
                    ? flaredBaseCenter
                    : ringCenters[segmentIndex];
            Vector3d[] bottomRing =
                CreateRing(
                    bottomRingCenter,
                    bottomWidthUnits,
                    bottomZ,
                    grid,
                    useOctagonalRings);
            Vector3d[] topRing =
                CreateRing(
                    ringCenters[segmentIndex + 1],
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

        Vector3d topCenter = new(
            ringCenters[^1].X,
            ringCenters[^1].Y,
            origin.Z +
            (trunkTopUnits * grid));

        return new GeneratedTrunk(
            parts,
            topCenter);
    }

    private static int CalculateBaseFlareAddedWidthUnits(
        int trunkWidthUnits,
        double baseFlare)
    {
        return Math.Clamp(
            (int)Math.Round(
                trunkWidthUnits *
                baseFlare,
                MidpointRounding.AwayFromZero),
            0,
            trunkWidthUnits);
    }

    private static Vector3d CreateBaseFlareCenter(
        Vector3d origin,
        int addedWidthUnits,
        GenerationSeed generationSeed,
        double grid)
    {
        if (addedWidthUnits == 0) {
            return origin;
        }

        DeterministicRandom random =
            new(
                generationSeed.Value ^
                BaseFlareSeedSalt);

        (int directionX, int directionY) =
            SelectCardinalDirection(
                random.NextInt32(4));

        double centerOffset =
            (addedWidthUnits * grid) /
            2.0;

        return new Vector3d(
            origin.X +
            (directionX * centerOffset),
            origin.Y +
            (directionY * centerOffset),
            origin.Z);
    }

    private static Vector3d[] CreateRingCenters(
        Vector3d origin,
        int trunkTopUnits,
        int segmentCount,
        double lean,
        double bend,
        GenerationSeed generationSeed,
        double grid)
    {
        DeterministicRandom random =
            new(
                generationSeed.Value ^
                DeformationSeedSalt);

        (int leanX, int leanY) =
            SelectCardinalDirection(
                random.NextInt32(4));

        int bendSign =
            random.NextBoolean()
                ? 1
                : -1;
        int bendX =
            -leanY *
            bendSign;
        int bendY =
            leanX *
            bendSign;

        int leanOffsetUnits =
            CalculateOffsetUnits(
                trunkTopUnits,
                lean);
        int bendOffsetUnits =
            CalculateOffsetUnits(
                trunkTopUnits,
                bend);
        Vector3d[] centers =
            new Vector3d[segmentCount + 1];

        for (
            int levelIndex = 0;
            levelIndex <= segmentCount;
            levelIndex++
        ) {
            double progress =
                levelIndex /
                (double)segmentCount;
            int appliedLeanUnits =
                (int)Math.Round(
                    leanOffsetUnits *
                    progress,
                    MidpointRounding.AwayFromZero);
            int appliedBendUnits =
                (int)Math.Round(
                    bendOffsetUnits *
                    Math.Sin(
                        Math.PI *
                        progress),
                    MidpointRounding.AwayFromZero);
            int xOffsetUnits =
                (leanX * appliedLeanUnits) +
                (bendX * appliedBendUnits);
            int yOffsetUnits =
                (leanY * appliedLeanUnits) +
                (bendY * appliedBendUnits);

            centers[levelIndex] = new Vector3d(
                origin.X +
                (xOffsetUnits * grid),
                origin.Y +
                (yOffsetUnits * grid),
                origin.Z);
        }

        return centers;
    }

    private static (int X, int Y) SelectCardinalDirection(
        int directionIndex)
    {
        return directionIndex switch
        {
            0 => (1, 0),
            1 => (0, 1),
            2 => (-1, 0),
            3 => (0, -1),
            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(directionIndex),
                    directionIndex,
                    "The trunk deformation direction is not recognized.")
        };
    }

    private static int CalculateOffsetUnits(
        int trunkTopUnits,
        double amount)
    {
        return (int)Math.Round(
            trunkTopUnits *
            amount,
            MidpointRounding.AwayFromZero);
    }

    private static void ValidateFraction(
        double value,
        double minimum,
        double maximum,
        string parameterName,
        string displayName)
    {
        if (
            !double.IsFinite(value) ||
            value < minimum ||
            value > maximum
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"The {displayName} must be between {minimum:P0} and {maximum:P0}.");
        }
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
        Vector3d center,
        int widthUnits,
        double z,
        double grid,
        bool useOctagonalRing)
    {
        // Widths can change from an even to an odd number of grid units.
        // Half-grid coordinates keep every ring centered on its path point.
        double halfWidth =
            (widthUnits * grid) /
            2.0;
        double minimumX =
            center.X -
            halfWidth;
        double minimumY =
            center.Y -
            halfWidth;
        double maximumX =
            center.X +
            halfWidth;
        double maximumY =
            center.Y +
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
