using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Input;

namespace BrushForge.Tests.Generation;

public sealed class TrunkCrossSectionControlTests
{
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
    public void ChangingCrossSectionPreservesTreeBoundsAndPartBounds()
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
            square.Bounds,
            octagonal.Bounds);
        Assert.Equal(
            square.Parts.Count,
            octagonal.Parts.Count);

        for (int index = 0; index < square.Parts.Count; index++) {
            Assert.Equal(
                square.Parts[index].Bounds,
                octagonal.Parts[index].Bounds);
        }
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
