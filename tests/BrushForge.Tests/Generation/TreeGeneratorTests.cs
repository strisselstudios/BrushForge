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

        Assert.Equal(5, result.BrushCount);
        Assert.Equal(TreeBrushRole.Trunk, result.Parts[0].Role);
        Assert.Equal(-1, result.Parts[0].CanopyLayerIndex);

        for (
            int layerIndex = 0;
            layerIndex < 4;
            layerIndex++
        ) {
            Assert.Equal(
                TreeBrushRole.Canopy,
                result.Parts[layerIndex + 1].Role);
            Assert.Equal(
                layerIndex,
                result.Parts[layerIndex + 1].CanopyLayerIndex);
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
    public void GenerateSnapsOriginAndVerticesToGrid()
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

        foreach (GeneratedTreeBrush part in result.Parts) {
            BrushValidationResult validation =
                ConvexBrushValidator.Validate(
                    part.Brush);

            Assert.True(validation.IsValid);

            Assert.All(
                validation.Geometry!.Vertices,
                vertex =>
                {
                    Assert.True(
                        GridSpacing.Eight.IsAligned(
                            vertex.X));
                    Assert.True(
                        GridSpacing.Eight.IsAligned(
                            vertex.Y));
                    Assert.True(
                        GridSpacing.Eight.IsAligned(
                            vertex.Z));
                });
        }
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

        Assert.All(
            result.Parts[0].Brush.Faces,
            face =>
                Assert.Equal(
                    "BARK",
                    face.TextureName));

        foreach (
            GeneratedTreeBrush canopyPart in
            result.Parts.Skip(1)
        ) {
            Assert.All(
                canopyPart.Brush.Faces,
                face =>
                    Assert.Equal(
                        "{LEAVES",
                        face.TextureName));
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
        string canopyTextureName = "LEAF")
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
            GridSpacing.Eight,
            trunkTextureName,
            canopyTextureName);
    }
}
