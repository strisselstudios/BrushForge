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
        Assert.Equal(
            TreeGenerationSettings.DefaultDetail,
            settings.Detail);
        Assert.Equal(
            0.0,
            TreeGenerationSettings.MinimumDetail);
        Assert.Equal(
            1.0,
            TreeGenerationSettings.MaximumDetail);
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

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ConstructorRejectsInvalidDetail(
        double detail)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    detail: detail));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void ConstructorAcceptsSupportedDetail(
        double detail)
    {
        TreeGenerationSettings settings =
            CreateSettings(
                detail: detail);

        Assert.Equal(detail, settings.Detail);
    }

    [Fact]
    public void WithMethodsPreserveUnchangedValues()
    {
        TreeGenerationSettings source =
            CreateSettings(
                detail: 0.375);

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
        Assert.Equal(source.Detail, edited.Detail);
    }

    [Fact]
    public void WithDetailChangesOnlyDetail()
    {
        TreeGenerationSettings source =
            CreateSettings(
                detail: 0.25);

        TreeGenerationSettings edited =
            source.WithDetail(
                0.75);

        Assert.Equal(0.25, source.Detail);
        Assert.Equal(0.75, edited.Detail);
        Assert.Equal(source.Origin, edited.Origin);
        Assert.Equal(source.OverallHeight, edited.OverallHeight);
        Assert.Equal(source.TrunkWidth, edited.TrunkWidth);
        Assert.Equal(source.CanopyWidth, edited.CanopyWidth);
        Assert.Equal(source.CanopyHeight, edited.CanopyHeight);
        Assert.Equal(source.CanopyLayerCount, edited.CanopyLayerCount);
        Assert.Equal(source.GenerationSeed, edited.GenerationSeed);
        Assert.Same(source.GridSpacing, edited.GridSpacing);
        Assert.Equal(source.TrunkTextureName, edited.TrunkTextureName);
        Assert.Equal(source.CanopyTextureName, edited.CanopyTextureName);
        Assert.Equal(source.TrunkSegmentCount, edited.TrunkSegmentCount);
        Assert.Equal(source.TrunkTaper, edited.TrunkTaper);
        Assert.Equal(source.TrunkLean, edited.TrunkLean);
        Assert.Equal(source.TrunkBend, edited.TrunkBend);
        Assert.Equal(source.TrunkBaseFlare, edited.TrunkBaseFlare);
        Assert.Equal(source.TrunkCrossSection, edited.TrunkCrossSection);
        Assert.Equal(source.TrunkIrregularity, edited.TrunkIrregularity);
        Assert.Equal(source.TrunkTwist, edited.TrunkTwist);
    }

    private static TreeGenerationSettings CreateSettings(
        double overallHeight = 256.0,
        double trunkWidth = 32.0,
        double canopyWidth = 160.0,
        double canopyHeight = 128.0,
        int canopyLayerCount = 3,
        double detail =
            TreeGenerationSettings.DefaultDetail)
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
            "LEAF",
            detail: detail);
    }
}
