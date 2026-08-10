using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;

namespace BrushForge.Tests.Generation;

public sealed class BranchProportionIntegrationTests
{
    private const double CoordinateTolerance = 1e-8;
    private const double MinimumPrimarySlenderness = 3.0;

    [Fact]
    public void GenerateLengthensPrimaryBranchesSublinearlyAsTrunkWidthIncreases()
    {
        TreeGenerationResult thin =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkWidth: 32.0));
        TreeGenerationResult thick =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkWidth: 64.0));
        Dictionary<string, double> thinLengths =
            GetBranchChordLengths(
                thin,
                primary: true);
        Dictionary<string, double> thickLengths =
            GetBranchChordLengths(
                thick,
                primary: true);

        Assert.True(
            thinLengths.Keys
                .OrderBy(
                    path =>
                        path,
                    StringComparer.Ordinal)
                .SequenceEqual(
                    thickLengths.Keys.OrderBy(
                        path =>
                            path,
                        StringComparer.Ordinal)));

        double[] ratios =
            thinLengths.Keys
                .Select(
                    path =>
                        thickLengths[path] /
                        thinLengths[path])
                .ToArray();

        Assert.All(
            ratios,
            ratio =>
                Assert.True(
                    ratio > 1.0));

        Assert.InRange(
            ratios.Average(),
            1.0 + CoordinateTolerance,
            1.95);
    }

    [Fact]
    public void GenerateMaintainsPrimarySlendernessForModeratelyThickTrunks()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkWidth: 64.0));
        Dictionary<string, double> lengths =
            GetBranchChordLengths(
                result,
                primary: true);
        GeneratedTreeBrush[] roots =
            GetPrimaryRootSegments(
                result);

        Assert.Equal(
            roots.Length,
            lengths.Count);

        foreach (GeneratedTreeBrush root in roots) {
            double startDiameter =
                GetStartHalfExtent(root) *
                2.0;
            double minimumLength =
                startDiameter *
                MinimumPrimarySlenderness;

            Assert.True(
                lengths[root.BranchPath!] +
                CoordinateTolerance >=
                minimumLength);
        }
    }

    [Fact]
    public void GenerateLengthensSecondaryBranchesWithLongerPrimaryParents()
    {
        TreeGenerationResult thin =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkWidth: 32.0));
        TreeGenerationResult thick =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkWidth: 64.0));
        Dictionary<string, double> thinLengths =
            GetBranchChordLengths(
                thin,
                primary: false);
        Dictionary<string, double> thickLengths =
            GetBranchChordLengths(
                thick,
                primary: false);

        Assert.Equal(
            thinLengths.Count,
            thickLengths.Count);
        Assert.True(
            thinLengths.Keys
                .OrderBy(
                    path =>
                        path,
                    StringComparer.Ordinal)
                .SequenceEqual(
                    thickLengths.Keys.OrderBy(
                        path =>
                            path,
                        StringComparer.Ordinal)));

        foreach (string path in thinLengths.Keys) {
            Assert.True(
                thickLengths[path] >
                thinLengths[path] +
                CoordinateTolerance);
        }
    }

    private static Dictionary<string, double> GetBranchChordLengths(
        TreeGenerationResult result,
        bool primary)
    {
        return result.Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Branch &&
                    part.BranchPath is not null &&
                    (
                        primary
                            ? !part.BranchPath.Contains('/')
                            : part.BranchPath.Contains('/')
                    ))
            .GroupBy(
                part =>
                    part.BranchPath!,
                StringComparer.Ordinal)
            .ToDictionary(
                group =>
                    group.Key,
                group =>
                {
                    GeneratedTreeBrush[] segments =
                        group
                            .OrderBy(
                                part =>
                                    part.BranchSegmentIndex)
                            .ToArray();

                    return GetStartCenter(
                            segments[0])
                        .DistanceTo(
                            GetEndCenter(
                                segments[^1]));
                },
                StringComparer.Ordinal);
    }

    private static GeneratedTreeBrush[] GetPrimaryRootSegments(
        TreeGenerationResult result)
    {
        return result.Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Branch &&
                    part.BranchPath is not null &&
                    !part.BranchPath.Contains('/') &&
                    part.BranchSegmentIndex == 0)
            .OrderBy(
                part =>
                    part.BranchPath!,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static Vector3d GetStartCenter(
        GeneratedTreeBrush branch)
    {
        PlanePoints3d cap =
            branch.Brush.Faces[0].PlanePoints;

        return (cap.Second + cap.Third) /
            2.0;
    }

    private static Vector3d GetEndCenter(
        GeneratedTreeBrush branch)
    {
        PlanePoints3d cap =
            branch.Brush.Faces[1].PlanePoints;

        return (cap.Second + cap.Third) /
            2.0;
    }

    private static double GetStartHalfExtent(
        GeneratedTreeBrush branch)
    {
        PlanePoints3d cap =
            branch.Brush.Faces[0].PlanePoints;

        return cap.First.DistanceTo(cap.Second) /
            2.0;
    }

    private static TreeGenerationSettings CreateSettings(
        double trunkWidth,
        ulong generationSeed = 83_417UL)
    {
        return new TreeGenerationSettings(
            Vector3d.Zero,
            overallHeight: 256.0,
            trunkWidth: trunkWidth,
            canopyWidth: 192.0,
            canopyHeight: 128.0,
            canopyLayerCount: 3,
            generationSeed:
                new GenerationSeed(
                    generationSeed),
            gridSpacing: GridSpacing.Eight,
            trunkTextureName: "WOOD",
            canopyTextureName: "LEAF",
            trunkSegmentCount: 6,
            trunkTaper: 0.0,
            trunkLean: 0.0,
            trunkBend: 0.0,
            trunkCrossSection:
                TrunkCrossSectionProfile.Square,
            trunkIrregularity: 0.0,
            trunkTwist: 0.0,
            detail: 1.0);
    }
}
