using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.MapFormat.Model;
using BrushForge.MapFormat.Serialization;
using BrushForge.MapFormat.Validation;

namespace BrushForge.Tests.Generation;

public sealed class TreeGeneratorTests
{
    [Fact]
    public void GenerateCreatesTrunkAndRequestedCanopyLayers()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    canopyLayerCount: 4));

        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();
        GeneratedTreeBrush[] canopyParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();

        Assert.Equal(3, trunkParts.Length);
        Assert.Equal(4, canopyParts.Length);
        Assert.Equal(7, result.BrushCount);
        Assert.All(
            trunkParts,
            part =>
                Assert.Equal(
                    -1,
                    part.CanopyLayerIndex));

        for (
            int layerIndex = 0;
            layerIndex < canopyParts.Length;
            layerIndex++
        ) {
            Assert.Equal(
                layerIndex,
                canopyParts[layerIndex].CanopyLayerIndex);
        }
    }

    [Fact]
    public void GenerateBuildsContiguousTaperedFacetedTrunkSegments()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings());
        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();

        Assert.Equal(3, trunkParts.Length);
        Assert.Equal(32.0, trunkParts[0].Bounds.Width);
        Assert.Equal(24.0, trunkParts[^1].Bounds.Width);
        Assert.All(
            trunkParts,
            part =>
            {
                Assert.Equal(10, part.Brush.FaceCount);
                Assert.True(
                    ConvexBrushValidator.Validate(
                        part.Brush)
                        .IsValid);
            });

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

    [Fact]
    public void GenerateProducesExportValidMapDocument()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings());

        MapExportValidationResult validation =
            MapExportValidator.Validate(
                result.Document);

        Assert.True(validation.IsValid);
        Assert.Single(result.Document.Entities);
        Assert.True(result.Document.Entities[0].IsWorldspawn);
    }

    [Fact]
    public void GenerateIsDeterministicForSameSettings()
    {
        TreeGenerationSettings settings =
            CreateSettings(
                generationSeed: 741UL,
                canopyLayerCount: 6);

        string first =
            Valve220MapWriter.Serialize(
                TreeGenerator.Generate(
                    settings)
                    .Document);

        string second =
            Valve220MapWriter.Serialize(
                TreeGenerator.Generate(
                    settings)
                    .Document);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GenerateUsesSeedToVaryCanopyGeometry()
    {
        TreeGenerationResult first =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 1UL,
                    canopyLayerCount: 8));

        TreeGenerationResult second =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 2UL,
                    canopyLayerCount: 8));

        Vector3d[] firstCenters =
            first.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .Select(
                    part =>
                        part.Bounds.Center)
                .ToArray();

        Vector3d[] secondCenters =
            second.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .Select(
                    part =>
                        part.Bounds.Center)
                .ToArray();

        Assert.False(
            firstCenters.SequenceEqual(
                secondCenters));
    }

    [Fact]
    public void GenerateSnapsOriginAndVerticesToSupportedGridPrecision()
    {
        TreeGenerationSettings settings =
            CreateSettings()
                .WithOrigin(
                    new Vector3d(
                        5.0,
                        11.0,
                        19.0));

        TreeGenerationResult result =
            TreeGenerator.Generate(
                settings);

        Assert.Equal(
            8.0,
            result.Parts[0].Bounds.Center.X);
        Assert.Equal(
            8.0,
            result.Parts[0].Bounds.Center.Y);
        Assert.Equal(
            16.0,
            result.Parts[0].Bounds.Minimum.Z);

        GridSpacing trunkHorizontalSpacing =
            new(
                settings.GridSpacing.Units /
                (
                    settings.TrunkCrossSection ==
                        TrunkCrossSectionProfile.Octagonal
                        ? 4.0
                        : 2.0
                ));

        foreach (GeneratedTreeBrush part in result.Parts) {
            BrushValidationResult validation =
                ConvexBrushValidator.Validate(
                    part.Brush);

            Assert.True(validation.IsValid);

            GridSpacing horizontalSpacing =
                part.Role == TreeBrushRole.Trunk
                    ? trunkHorizontalSpacing
                    : settings.GridSpacing;

            Assert.All(
                validation.Geometry!.Vertices,
                vertex =>
                {
                    Assert.True(
                        horizontalSpacing.IsAligned(
                            vertex.X));
                    Assert.True(
                        horizontalSpacing.IsAligned(
                            vertex.Y));
                    Assert.True(
                        settings.GridSpacing.IsAligned(
                            vertex.Z));
                });
        }
    }

    [Fact]
    public void GenerateKeepsEveryTrunkSegmentCenteredOnSnappedOrigin()
    {
        TreeGenerationSettings settings =
            CreateSettings()
                .WithOrigin(
                    new Vector3d(
                        5.0,
                        11.0,
                        19.0));

        TreeGenerationResult result =
            TreeGenerator.Generate(
                settings);

        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();

        Assert.True(
            trunkParts.Length >= 2);

        Assert.All(
            trunkParts,
            part =>
            {
                Assert.Equal(
                    8.0,
                    part.Bounds.Center.X,
                    precision: 6);
                Assert.Equal(
                    8.0,
                    part.Bounds.Center.Y,
                    precision: 6);
            });
    }

    [Fact]
    public void GenerateTapersOppositeTrunkSidesByEqualDistances()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings());

        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();

        Assert.True(
            trunkParts.Length >= 2);

        var baseBounds =
            trunkParts[0].Bounds;

        var topBounds =
            trunkParts[^1].Bounds;

        double negativeXReduction =
            topBounds.Minimum.X -
            baseBounds.Minimum.X;

        double positiveXReduction =
            baseBounds.Maximum.X -
            topBounds.Maximum.X;

        double negativeYReduction =
            topBounds.Minimum.Y -
            baseBounds.Minimum.Y;

        double positiveYReduction =
            baseBounds.Maximum.Y -
            topBounds.Maximum.Y;

        Assert.True(
            negativeXReduction > 0.0);
        Assert.True(
            negativeYReduction > 0.0);

        Assert.Equal(
            negativeXReduction,
            positiveXReduction,
            precision: 6);

        Assert.Equal(
            negativeYReduction,
            positiveYReduction,
            precision: 6);

        Assert.Equal(
            baseBounds.Center.X,
            topBounds.Center.X,
            precision: 6);

        Assert.Equal(
            baseBounds.Center.Y,
            topBounds.Center.Y,
            precision: 6);
    }
    [Fact]
    public void GeneratePreservesRequestedOverallHeightAfterSnapping()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    overallHeight: 259.0));

        Assert.Equal(256.0, result.Bounds.Height);
    }

    [Fact]
    public void GenerateAssignsTexturesBySemanticRole()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkTextureName: "BARK",
                    canopyTextureName: "{LEAVES"));

        foreach (GeneratedTreeBrush part in result.Parts) {
            string expectedTexture =
                part.Role == TreeBrushRole.Trunk
                    ? "BARK"
                    : "{LEAVES";

            Assert.All(
                part.Brush.Faces,
                face =>
                    Assert.Equal(
                        expectedTexture,
                        face.TextureName));
        }
    }

    [Theory]
    [InlineData(0.0, 6)]
    [InlineData(0.49, 6)]
    [InlineData(0.50, 10)]
    [InlineData(1.0, 10)]
    public void GenerateUsesDetailToResolveOctagonalTrunkComplexity(
        double detail,
        int expectedFaceCount)
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Octagonal,
                    detail: detail));

        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();

        Assert.All(
            trunkParts,
            part =>
                Assert.Equal(
                    expectedFaceCount,
                    part.Brush.FaceCount));
    }

    [Fact]
    public void GenerateDoesNotIncreaseExplicitSquareTrunkComplexity()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    detail: 1.0));

        Assert.All(
            result.Parts.Where(
                part =>
                    part.Role == TreeBrushRole.Trunk),
            part =>
                Assert.Equal(
                    6,
                    part.Brush.FaceCount));
    }

    [Fact]
    public void ChangingDetailPreservesTreeAndPartBounds()
    {
        TreeGenerationResult minimum =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Octagonal,
                    detail: 0.0));

        TreeGenerationResult maximum =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Octagonal,
                    detail: 1.0));

        Assert.Equal(
            maximum.Bounds,
            minimum.Bounds);
        Assert.Equal(
            maximum.Parts.Count,
            minimum.Parts.Count);

        for (int index = 0; index < maximum.Parts.Count; index++) {
            Assert.Equal(
                maximum.Parts[index].Bounds,
                minimum.Parts[index].Bounds);
        }
    }

    [Fact]
    public void GenerateStoresGeneratorMetadataInWorldspawn()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 123456UL));

        MapEntity worldspawn =
            Assert.IsType<MapEntity>(
                result.Document.Worldspawn);

        Assert.True(
            worldspawn.TryGetProperty(
                "_brushforge_generator",
                out string? generator));

        Assert.True(
            worldspawn.TryGetProperty(
                "_brushforge_seed",
                out string? seed));

        Assert.Equal("tree", generator);
        Assert.Equal("123456", seed);
    }

    private static TreeGenerationSettings CreateSettings(
        double overallHeight = 256.0,
        double trunkWidth = 32.0,
        double canopyWidth = 160.0,
        double canopyHeight = 128.0,
        int canopyLayerCount = 3,
        ulong generationSeed = 1UL,
        string trunkTextureName = "WOOD",
        string canopyTextureName = "LEAF",
        GridSpacing? gridSpacing = null,
        TrunkCrossSectionProfile trunkCrossSection =
            TreeGenerationSettings.DefaultTrunkCrossSection,
        double detail = TreeGenerationSettings.DefaultDetail)
    {
        return new TreeGenerationSettings(
            Vector3d.Zero,
            overallHeight,
            trunkWidth,
            canopyWidth,
            canopyHeight,
            canopyLayerCount,
            new GenerationSeed(
                generationSeed),
            gridSpacing ?? GridSpacing.Eight,
            trunkTextureName,
            canopyTextureName,
            trunkCrossSection: trunkCrossSection,
            detail: detail);
    }
}
