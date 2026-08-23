using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Input;

namespace BrushForge.Tests.Generation;

public sealed class TrunkCrossSectionControlTests
{
    private const double CoordinateTolerance = 1e-8;

    [Fact]
    public void SettingsUseOctagonalTrunkCrossSectionByDefault()
    {
        TreeGenerationSettings settings =
            CreateSettings();

        Assert.Equal(
            TrunkCrossSectionProfile.Octagonal,
            settings.TrunkCrossSection);
    }

    [Fact]
    public void SettingsRejectUndefinedTrunkCrossSection()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkCrossSection:
                        (TrunkCrossSectionProfile)999));
    }

    [Fact]
    public void ParserReadsExplicitTrunkCrossSection()
    {
        TreeGenerationSettings settings =
            TreeGenerationInputParser.Parse(
                new TreeGenerationInput(
                    "123",
                    "256",
                    "32",
                    "160",
                    "128",
                    "3",
                    "8",
                    "WOOD",
                    "LEAF",
                    TrunkCrossSection: "Square"));

        Assert.Equal(
            TrunkCrossSectionProfile.Square,
            settings.TrunkCrossSection);
    }

    [Fact]
    public void SquareProfileBuildsSixFaceTrunkBrushes()
    {
        GeneratedTreeBrush[] trunkParts =
            GenerateTrunkParts(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square));

        Assert.All(
            trunkParts,
            part =>
                Assert.Equal(
                    6,
                    part.Brush.FaceCount));
    }

    [Fact]
    public void OctagonalProfileBuildsTenFaceTrunkAtMinimumWidthAndLargestGrid()
    {
        GeneratedTreeBrush[] trunkParts =
            GenerateTrunkParts(
                CreateSettings(
                    trunkWidth: 16.0,
                    gridSpacing: new GridSpacing(16.0),
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Octagonal));

        Assert.All(
            trunkParts,
            part =>
            {
                Assert.Equal(
                    10,
                    part.Brush.FaceCount);
                Assert.True(
                    ConvexBrushValidator.Validate(
                        part.Brush)
                        .IsValid);
            });
    }

    [Fact]
    public void OctagonalProfileSideCountIsIndependentOfGridSpacing()
    {
        double[] gridValues =
        [
            4.0,
            8.0,
            16.0
        ];

        foreach (double gridValue in gridValues) {
            GeneratedTreeBrush[] trunkParts =
                GenerateTrunkParts(
                    CreateSettings(
                        trunkWidth: 16.0,
                        gridSpacing:
                            new GridSpacing(gridValue),
                        trunkCrossSection:
                            TrunkCrossSectionProfile.Octagonal));

            Assert.All(
                trunkParts,
                part =>
                    Assert.Equal(
                        10,
                        part.Brush.FaceCount));
        }
    }

    [Fact]
    public void ChangingCrossSectionPreservesNonBranchGeometryAndBranchThicknessWhileBranchesFollowSurface()
    {
        TreeGenerationResult square =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square));
        TreeGenerationResult octagonal =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Octagonal));

        Assert.Equal(
            square.Bounds.Minimum.Z,
            octagonal.Bounds.Minimum.Z);
        Assert.Equal(
            square.Bounds.Maximum.Z,
            octagonal.Bounds.Maximum.Z);
        Assert.Equal(
            square.Parts.Count,
            octagonal.Parts.Count);

        bool branchSurfacePositionChanged = false;

        for (int index = 0; index < square.Parts.Count; index++) {
            GeneratedTreeBrush squarePart =
                square.Parts[index];
            GeneratedTreeBrush octagonalPart =
                octagonal.Parts[index];

            Assert.Equal(
                squarePart.Role,
                octagonalPart.Role);
            Assert.Equal(
                squarePart.CanopyLayerIndex,
                octagonalPart.CanopyLayerIndex);
            Assert.Equal(
                squarePart.BranchPath,
                octagonalPart.BranchPath);
            Assert.Equal(
                squarePart.BranchSegmentIndex,
                octagonalPart.BranchSegmentIndex);
            Assert.Equal(
                squarePart.BranchSegmentCount,
                octagonalPart.BranchSegmentCount);
            if (squarePart.Role != TreeBrushRole.Branch) {
                Assert.Equal(
                    squarePart.Bounds,
                    octagonalPart.Bounds);
                continue;
            }

            AssertApproximatelyEqual(
                GetStartHalfExtent(squarePart),
                GetStartHalfExtent(octagonalPart));
            AssertApproximatelyEqual(
                GetEndHalfExtent(squarePart),
                GetEndHalfExtent(octagonalPart));

            branchSurfacePositionChanged |=
                !AreVectorsApproximatelyEqual(
                    squarePart.Bounds.Center,
                    octagonalPart.Bounds.Center);
        }

        Assert.True(branchSurfacePositionChanged);
    }

    [Fact]
    public void MaximumOctagonalShapeControlsRemainConvexAndConnected()
    {
        GeneratedTreeBrush[] trunkParts =
            GenerateTrunkParts(
                CreateSettings(
                    trunkWidth: 96.0,
                    gridSpacing: new GridSpacing(16.0),
                    trunkSegmentCount: 6,
                    trunkTaper:
                        TreeGenerationSettings.MaximumTrunkTaper,
                    trunkLean:
                        TreeGenerationSettings.MaximumTrunkLean,
                    trunkBend:
                        TreeGenerationSettings.MaximumTrunkBend,
                    trunkBaseFlare:
                        TreeGenerationSettings.MaximumTrunkBaseFlare,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Octagonal));

        Assert.Equal(
            6,
            trunkParts.Length);

        Assert.All(
            trunkParts,
            part =>
            {
                Assert.Equal(
                    10,
                    part.Brush.FaceCount);
                Assert.True(
                    ConvexBrushValidator.Validate(
                        part.Brush)
                        .IsValid);
            });

        for (int index = 1; index < trunkParts.Length; index++) {
            Assert.Equal(
                trunkParts[index - 1].Bounds.Maximum.Z,
                trunkParts[index].Bounds.Minimum.Z);
        }
    }

    private static void AssertApproximatelyEqual(
        double expected,
        double actual)
    {
        Assert.InRange(
            Math.Abs(actual - expected),
            0.0,
            CoordinateTolerance);
    }

    private static double GetStartHalfExtent(
        GeneratedTreeBrush branch)
    {
        PlanePoints3d cap =
            branch.Brush.Faces[0].PlanePoints;

        return cap.First.DistanceTo(cap.Second) /
            2.0;
    }

    private static double GetEndHalfExtent(
        GeneratedTreeBrush branch)
    {
        PlanePoints3d cap =
            branch.Brush.Faces[1].PlanePoints;

        return cap.First.DistanceTo(cap.Second) /
            2.0;
    }

    private static bool AreVectorsApproximatelyEqual(
        Vector3d first,
        Vector3d second)
    {
        return
            Math.Abs(first.X - second.X) <= CoordinateTolerance &&
            Math.Abs(first.Y - second.Y) <= CoordinateTolerance &&
            Math.Abs(first.Z - second.Z) <= CoordinateTolerance;
    }

    private static GeneratedTreeBrush[] GenerateTrunkParts(
        TreeGenerationSettings settings)
    {
        return TreeGenerator
            .Generate(settings)
            .Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Trunk)
            .ToArray();
    }

    private static TreeGenerationSettings CreateSettings(
        double trunkWidth = 32.0,
        GridSpacing? gridSpacing = null,
        int trunkSegmentCount =
            TreeGenerationSettings.DefaultTrunkSegmentCount,
        double trunkTaper =
            TreeGenerationSettings.DefaultTrunkTaper,
        double trunkLean =
            TreeGenerationSettings.DefaultTrunkLean,
        double trunkBend =
            TreeGenerationSettings.DefaultTrunkBend,
        double trunkBaseFlare =
            TreeGenerationSettings.DefaultTrunkBaseFlare,
        TrunkCrossSectionProfile trunkCrossSection =
            TreeGenerationSettings.DefaultTrunkCrossSection)
    {
        return new TreeGenerationSettings(
            Vector3d.Zero,
            256.0,
            trunkWidth,
            160.0,
            128.0,
            3,
            new GenerationSeed(123UL),
            gridSpacing ?? GridSpacing.Eight,
            "WOOD",
            "LEAF",
            trunkSegmentCount,
            trunkTaper,
            trunkLean,
            trunkBend,
            trunkBaseFlare,
            trunkCrossSection);
    }
}
