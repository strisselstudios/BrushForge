using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Input;

namespace BrushForge.Tests.Generation;

public sealed class TreeGenerationInputParserTests
{
    [Fact]
    public void ParseCreatesValidatedSettings()
    {
        TreeGenerationSettings settings =
            TreeGenerationInputParser.Parse(
                CreateInput());

        Assert.Equal(42UL, settings.GenerationSeed.Value);
        Assert.Equal(256.0, settings.OverallHeight);
        Assert.Equal(32.0, settings.TrunkWidth);
        Assert.Equal(160.0, settings.CanopyWidth);
        Assert.Equal(128.0, settings.CanopyHeight);
        Assert.Equal(3, settings.CanopyLayerCount);
        Assert.Equal(8.0, settings.GridSpacing.Units);
        Assert.Equal("WOOD", settings.TrunkTextureName);
        Assert.Equal("LEAF", settings.CanopyTextureName);
    }

    [Fact]
    public void ParseUsesInvariantDecimalSeparator()
    {
        TreeGenerationInput input =
            CreateInput() with
            {
                OverallHeight = "256.5"
            };

        TreeGenerationSettings settings =
            TreeGenerationInputParser.Parse(
                input);

        Assert.Equal(256.5, settings.OverallHeight);
    }

    [Fact]
    public void ParseRejectsInvalidGenerationSeed()
    {
        TreeGenerationInput input =
            CreateInput() with
            {
                GenerationSeed = "not-a-seed"
            };

        Assert.Throws<FormatException>(
            () =>
                TreeGenerationInputParser.Parse(
                    input));
    }

    [Fact]
    public void ParseRejectsInvalidDimension()
    {
        TreeGenerationInput input =
            CreateInput() with
            {
                CanopyWidth = "wide"
            };

        Assert.Throws<FormatException>(
            () =>
                TreeGenerationInputParser.Parse(
                    input));
    }

    [Fact]
    public void ParseRejectsNonPositiveGridSpacing()
    {
        TreeGenerationInput input =
            CreateInput() with
            {
                GridSpacing = "0"
            };

        Assert.Throws<FormatException>(
            () =>
                TreeGenerationInputParser.Parse(
                    input));
    }

    [Fact]
    public void ParseRejectsNonWholeLayerCount()
    {
        TreeGenerationInput input =
            CreateInput() with
            {
                CanopyLayerCount = "3.5"
            };

        Assert.Throws<FormatException>(
            () =>
                TreeGenerationInputParser.Parse(
                    input));
    }

    [Fact]
    public void ParsePreservesTreeSettingsValidation()
    {
        TreeGenerationInput input =
            CreateInput() with
            {
                OverallHeight = "128",
                CanopyHeight = "128"
            };

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                TreeGenerationInputParser.Parse(
                    input));
    }

    private static TreeGenerationInput CreateInput()
    {
        return new TreeGenerationInput(
            "42",
            "256",
            "32",
            "160",
            "128",
            "3",
            "8",
            "WOOD",
            "LEAF");
    }
}
