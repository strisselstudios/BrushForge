using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.ProjectModel.Projects;

namespace BrushForge.Tests.Generation;

public sealed class TreeGenerationSettingsTests
{
    [Fact]
    public void CreateDefaultUsesProjectSeedAndGrid()
    {
        GenerationSeed seed =
            new(8128UL);

        BrushForgeProjectSettings projectSettings =
            BrushForgeProjectSettings.CreateDefault(
                seed)
                .WithGridSpacing(
                    GridSpacing.Sixteen);

        TreeGenerationSettings settings =
            TreeGenerationSettings.CreateDefault(
                projectSettings);

        Assert.Equal(seed, settings.GenerationSeed);
        Assert.Same(
            GridSpacing.Sixteen,
            settings.GridSpacing);
        Assert.Equal(3, settings.CanopyLayerCount);
        Assert.Equal("WOOD", settings.TrunkTextureName);
        Assert.Equal("LEAF", settings.CanopyTextureName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidOverallHeight(
        double overallHeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    overallHeight: overallHeight));
    }

    [Fact]
    public void ConstructorRejectsCanopyAtLeastOverallHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    overallHeight: 128.0,
                    canopyHeight: 128.0));
    }

    [Fact]
    public void ConstructorRejectsTrunkWiderThanCanopy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkWidth: 192.0,
                    canopyWidth: 128.0));
    }

    [Fact]
    public void ConstructorRejectsLayerCountOutsideSupportedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    canopyLayerCount: 0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    canopyLayerCount: 9));
    }

    [Fact]
    public void ConstructorRequiresOneGridUnitPerLayer()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    canopyHeight: 16.0,
                    canopyLayerCount: 3));
    }

    [Fact]
    public void WithMethodsPreserveUnchangedValues()
    {
        TreeGenerationSettings source =
            CreateSettings();

        GenerationSeed replacementSeed =
            new(99UL);

        TreeGenerationSettings edited =
            source
                .WithOrigin(
                    new Vector3d(
                        12.0,
                        24.0,
                        36.0))
                .WithGenerationSeed(
                    replacementSeed)
                .WithTextures(
                    "TRUNK",
                    "CANOPY");

        Assert.Equal(
            new Vector3d(
                12.0,
                24.0,
                36.0),
            edited.Origin);
        Assert.Equal(replacementSeed, edited.GenerationSeed);
        Assert.Equal("TRUNK", edited.TrunkTextureName);
        Assert.Equal("CANOPY", edited.CanopyTextureName);
        Assert.Equal(source.OverallHeight, edited.OverallHeight);
        Assert.Equal(source.CanopyLayerCount, edited.CanopyLayerCount);
        Assert.Same(source.GridSpacing, edited.GridSpacing);
    }

    private static TreeGenerationSettings CreateSettings(
        double overallHeight = 256.0,
        double trunkWidth = 32.0,
        double canopyWidth = 160.0,
        double canopyHeight = 128.0,
        int canopyLayerCount = 3)
    {
        return new TreeGenerationSettings(
            Vector3d.Zero,
            overallHeight,
            trunkWidth,
            canopyWidth,
            canopyHeight,
            canopyLayerCount,
            new GenerationSeed(1UL),
            GridSpacing.Eight,
            "WOOD",
            "LEAF");
    }
}
