using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Input;

namespace BrushForge.Tests.Generation;

public sealed class TrunkShapeControlTests
{
    [Fact]
    public void SettingsUseDocumentedTrunkShapeDefaults()
    {
        TreeGenerationSettings settings =
            CreateSettings();

        Assert.Equal(
            TreeGenerationSettings.DefaultTrunkSegmentCount,
            settings.TrunkSegmentCount);
        Assert.Equal(
            TreeGenerationSettings.DefaultTrunkTaper,
            settings.TrunkTaper);
    }

    [Fact]
    public void SettingsRejectUnsupportedTrunkShapeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkSegmentCount: 1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkSegmentCount: 7));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkTaper: -0.01));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkTaper: 0.76));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkTaper: double.NaN));
    }

    [Fact]
    public void ParserReadsExplicitTrunkShapeControls()
    {
        TreeGenerationSettings settings =
            TreeGenerationInputParser.Parse(
                new TreeGenerationInput(
                    "42",
                    "256",
                    "32",
                    "160",
                    "128",
                    "3",
                    "8",
                    "WOOD",
                    "LEAF",
                    "6",
                    "50"));

        Assert.Equal(6, settings.TrunkSegmentCount);
        Assert.Equal(0.5, settings.TrunkTaper);
    }

    [Fact]
    public void GeneratorUsesRequestedTrunkSegmentCount()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6));

        GeneratedTreeBrush[] trunkParts =
            GetTrunkParts(
                result);

        Assert.Equal(6, trunkParts.Length);
    }

    [Fact]
    public void ZeroTaperKeepsTrunkWidthConstant()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkTaper: 0.0));

        GeneratedTreeBrush[] trunkParts =
            GetTrunkParts(
                result);

        Assert.All(
            trunkParts,
            part =>
                Assert.Equal(
                    32.0,
                    part.Bounds.Width));
    }

    [Fact]
    public void FiftyPercentTaperNarrowsTopAndKeepsSegmentsValid()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkTaper: 0.5));

        GeneratedTreeBrush[] trunkParts =
            GetTrunkParts(
                result);

        Assert.Equal(6, trunkParts.Length);
        Assert.Equal(
            16.0,
            MeasureTopRingWidth(
                trunkParts[^1]));

        Assert.All(
            trunkParts,
            part =>
                Assert.True(
                    ConvexBrushValidator.Validate(
                        part.Brush)
                        .IsValid));

        for (
            int segmentIndex = 1;
            segmentIndex < trunkParts.Length;
            segmentIndex++
        ) {
            Assert.Equal(
                trunkParts[segmentIndex - 1].Bounds.Maximum.Z,
                trunkParts[segmentIndex].Bounds.Minimum.Z);
        }
    }

    private static GeneratedTreeBrush[] GetTrunkParts(
        TreeGenerationResult result)
    {
        return result.Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Trunk)
            .ToArray();
    }

    private static double MeasureTopRingWidth(
        GeneratedTreeBrush part)
    {
        BrushValidationResult validation =
            ConvexBrushValidator.Validate(
                part.Brush);

        Assert.True(validation.IsValid);

        Vector3d[] vertices =
            validation.Geometry!
                .Vertices
                .ToArray();

        double maximumZ =
            vertices.Max(
                vertex =>
                    vertex.Z);

        Vector3d[] topVertices =
            vertices
                .Where(
                    vertex =>
                        Math.Abs(
                            vertex.Z -
                            maximumZ) < 1e-8)
                .ToArray();

        return
            topVertices.Max(
                vertex =>
                    vertex.X) -
            topVertices.Min(
                vertex =>
                    vertex.X);
    }

    private static TreeGenerationSettings CreateSettings(
        int trunkSegmentCount =
            TreeGenerationSettings.DefaultTrunkSegmentCount,
        double trunkTaper =
            TreeGenerationSettings.DefaultTrunkTaper)
    {
        return new TreeGenerationSettings(
            Vector3d.Zero,
            256.0,
            32.0,
            160.0,
            128.0,
            3,
            new GenerationSeed(1UL),
            GridSpacing.Eight,
            "WOOD",
            "LEAF",
            trunkSegmentCount,
            trunkTaper);
    }
}
