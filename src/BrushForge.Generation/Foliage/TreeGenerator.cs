using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Geometry;
using BrushForge.MapFormat.Model;

namespace BrushForge.Generation.Foliage;

/// <summary>
/// Generates a low-brush-count segmented trunk and layered box canopy
/// suitable for preview and Valve 220 export.
/// </summary>
public static class TreeGenerator
{
    public static TreeGenerationResult Generate(
        TreeGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        GridSpacing gridSpacing =
            settings.GridSpacing;

        double grid =
            gridSpacing.Units;

        Vector3d origin = new(
            gridSpacing.Snap(settings.Origin.X),
            gridSpacing.Snap(settings.Origin.Y),
            gridSpacing.Snap(settings.Origin.Z));

        int overallHeightUnits =
            ToGridUnits(
                settings.OverallHeight,
                gridSpacing,
                minimumUnits: 2);

        int canopyHeightUnits =
            ToGridUnits(
                settings.CanopyHeight,
                gridSpacing,
                settings.CanopyLayerCount);

        canopyHeightUnits = Math.Min(
            canopyHeightUnits,
            overallHeightUnits - 1);

        int trunkWidthUnits =
            ToGridUnits(
                settings.TrunkWidth,
                gridSpacing,
                minimumUnits: 1);

        int canopyWidthUnits = Math.Max(
            ToGridUnits(
                settings.CanopyWidth,
                gridSpacing,
                minimumUnits: 2),
            trunkWidthUnits);

        int canopyBottomUnits =
            overallHeightUnits -
            canopyHeightUnits;

        int trunkTopUnits = Math.Min(
            overallHeightUnits,
            canopyBottomUnits + 1);

        GeneratedTrunk trunk =
            TaperedTrunkGenerator.Generate(
                origin,
                trunkWidthUnits,
                trunkTopUnits,
                settings.TrunkSegmentCount,
                settings.TrunkTaper,
                settings.TrunkLean,
                settings.TrunkBend,
                settings.TrunkBaseFlare,
                settings.TrunkCrossSection,
                settings.GenerationSeed,
                grid,
                settings.TrunkTextureName);

        List<GeneratedTreeBrush> parts =
            trunk.Parts.ToList();

        DeterministicRandom random =
            new(settings.GenerationSeed);

        int minimumCanopyWidthUnits = Math.Min(
            canopyWidthUnits,
            trunkWidthUnits + 2);

        int baseLayerHeightUnits =
            canopyHeightUnits /
            settings.CanopyLayerCount;

        int remainingHeightUnits =
            canopyHeightUnits %
            settings.CanopyLayerCount;

        int currentBottomUnits =
            canopyBottomUnits;

        for (
            int layerIndex = 0;
            layerIndex < settings.CanopyLayerCount;
            layerIndex++
        ) {
            int layerHeightUnits =
                baseLayerHeightUnits +
                (
                    layerIndex < remainingHeightUnits
                        ? 1
                        : 0
                );

            int layerWidthUnits =
                CalculateLayerWidthUnits(
                    layerIndex,
                    settings.CanopyLayerCount,
                    canopyWidthUnits,
                    minimumCanopyWidthUnits);

            int maximumDepthReduction = Math.Min(
                2,
                layerWidthUnits - minimumCanopyWidthUnits);

            int depthReduction =
                maximumDepthReduction == 0
                    ? 0
                    : random.NextInt32(
                        maximumDepthReduction + 1);

            int layerDepthUnits =
                layerWidthUnits -
                depthReduction;

            int maximumXOffsetUnits = Math.Min(
                2,
                Math.Max(
                    0,
                    (canopyWidthUnits - layerWidthUnits) / 2));

            int maximumYOffsetUnits = Math.Min(
                2,
                Math.Max(
                    0,
                    (canopyWidthUnits - layerDepthUnits) / 2));

            int xOffsetUnits =
                NextSignedOffset(
                    random,
                    maximumXOffsetUnits);

            int yOffsetUnits =
                NextSignedOffset(
                    random,
                    maximumYOffsetUnits);

            Vector3d layerCenter = new(
                trunk.TopCenter.X +
                (xOffsetUnits * grid),
                trunk.TopCenter.Y +
                (yOffsetUnits * grid),
                origin.Z);

            double layerMinimumZ =
                origin.Z +
                (currentBottomUnits * grid);

            currentBottomUnits +=
                layerHeightUnits;

            double layerMaximumZ =
                origin.Z +
                (currentBottomUnits * grid);

            Bounds3d canopyBounds =
                CreateCenteredBounds(
                    layerCenter,
                    layerWidthUnits,
                    layerDepthUnits,
                    layerMinimumZ,
                    layerMaximumZ,
                    grid);

            ConvexBrush canopyBrush =
                AxisAlignedBoxBrushFactory.Create(
                    canopyBounds,
                    settings.CanopyTextureName);

            parts.Add(
                new GeneratedTreeBrush(
                    TreeBrushRole.Canopy,
                    layerIndex,
                    canopyBrush,
                    canopyBounds));
        }

        ConvexBrush[] brushes =
            parts
                .Select(
                    part =>
                        part.Brush)
                .ToArray();

        MapEntity worldspawn =
            MapEntity.CreateWorldspawn(
                brushes,
            [
                new MapProperty(
                    "_brushforge_generator",
                    "tree"),
                new MapProperty(
                    "_brushforge_seed",
                    settings.GenerationSeed.ToString())
            ]);

        MapDocument document = new(
        [
            worldspawn
        ]);

        return new TreeGenerationResult(
            parts,
            document);
    }

    private static int ToGridUnits(
        double value,
        GridSpacing gridSpacing,
        int minimumUnits)
    {
        double rounded = Math.Round(
            value / gridSpacing.Units,
            MidpointRounding.AwayFromZero);

        int units = checked((int)rounded);

        return Math.Max(
            minimumUnits,
            units);
    }

    private static int CalculateLayerWidthUnits(
        int layerIndex,
        int layerCount,
        int canopyWidthUnits,
        int minimumCanopyWidthUnits)
    {
        if (layerCount == 1) {
            return canopyWidthUnits;
        }

        int distanceFromCenter = Math.Abs(
            (2 * layerIndex) -
            (layerCount - 1));

        int taperUnits =
            (canopyWidthUnits * distanceFromCenter) /
            (4 * (layerCount - 1));

        return Math.Max(
            minimumCanopyWidthUnits,
            canopyWidthUnits - taperUnits);
    }

    private static int NextSignedOffset(
        DeterministicRandom random,
        int maximumMagnitude)
    {
        if (maximumMagnitude == 0) {
            return 0;
        }

        return random.NextInt32(
            -maximumMagnitude,
            maximumMagnitude + 1);
    }

    private static Bounds3d CreateCenteredBounds(
        Vector3d center,
        int widthUnits,
        int depthUnits,
        double minimumZ,
        double maximumZ,
        double grid)
    {
        double minimumX =
            center.X -
            ((widthUnits / 2) * grid);

        double minimumY =
            center.Y -
            ((depthUnits / 2) * grid);

        return new Bounds3d(
            new Vector3d(
                minimumX,
                minimumY,
                minimumZ),
            new Vector3d(
                minimumX + (widthUnits * grid),
                minimumY + (depthUnits * grid),
                maximumZ));
    }
}
