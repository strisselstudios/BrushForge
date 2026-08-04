using BrushForge.Core.Grid;
using BrushForge.Core.Numerics;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.ProjectModel.Projects;

namespace BrushForge.Generation.Foliage;

/// <summary>
/// Immutable input settings for the first deterministic brush-tree generator.
/// </summary>
public sealed record TreeGenerationSettings
{
    public const int MinimumCanopyLayerCount = 1;
    public const int MaximumCanopyLayerCount = 8;
    public const int MaximumGridUnitCount = 1_000_000;
    public const double MaximumDimension = 131_072.0;

    public TreeGenerationSettings(
        Vector3d origin,
        double overallHeight,
        double trunkWidth,
        double canopyWidth,
        double canopyHeight,
        int canopyLayerCount,
        GenerationSeed generationSeed,
        GridSpacing gridSpacing,
        string trunkTextureName,
        string canopyTextureName)
    {
        if (!origin.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(origin),
                origin,
                "The tree origin must be finite.");
        }

        ArgumentNullException.ThrowIfNull(gridSpacing);

        ValidateDimension(
            overallHeight,
            nameof(overallHeight));

        ValidateDimension(
            trunkWidth,
            nameof(trunkWidth));

        ValidateDimension(
            canopyWidth,
            nameof(canopyWidth));

        ValidateDimension(
            canopyHeight,
            nameof(canopyHeight));

        if (
            canopyLayerCount < MinimumCanopyLayerCount ||
            canopyLayerCount > MaximumCanopyLayerCount
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(canopyLayerCount),
                canopyLayerCount,
                $"Canopy layer count must be between {MinimumCanopyLayerCount} and {MaximumCanopyLayerCount}.");
        }

        if (canopyHeight >= overallHeight) {
            throw new ArgumentOutOfRangeException(
                nameof(canopyHeight),
                canopyHeight,
                "Canopy height must be less than overall tree height.");
        }

        if (trunkWidth > canopyWidth) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkWidth),
                trunkWidth,
                "Trunk width cannot exceed canopy width.");
        }

        double minimumCanopyHeight =
            gridSpacing.Units *
            canopyLayerCount;

        if (canopyHeight < minimumCanopyHeight) {
            throw new ArgumentOutOfRangeException(
                nameof(canopyHeight),
                canopyHeight,
                "Canopy height must provide at least one grid unit per canopy layer.");
        }

        if (
            overallHeight - canopyHeight <
            gridSpacing.Units
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(overallHeight),
                overallHeight,
                "The tree requires at least one grid unit below the canopy.");
        }

        ValidateGridRepresentableDimension(
            overallHeight,
            gridSpacing,
            nameof(overallHeight));

        ValidateGridRepresentableDimension(
            trunkWidth,
            gridSpacing,
            nameof(trunkWidth));

        ValidateGridRepresentableDimension(
            canopyWidth,
            gridSpacing,
            nameof(canopyWidth));

        ValidateGridRepresentableDimension(
            canopyHeight,
            gridSpacing,
            nameof(canopyHeight));

        Origin = origin;
        OverallHeight = overallHeight;
        TrunkWidth = trunkWidth;
        CanopyWidth = canopyWidth;
        CanopyHeight = canopyHeight;
        CanopyLayerCount = canopyLayerCount;
        GenerationSeed = generationSeed;
        GridSpacing = gridSpacing;
        TrunkTextureName = NormalizeTextureName(
            trunkTextureName,
            nameof(trunkTextureName));
        CanopyTextureName = NormalizeTextureName(
            canopyTextureName,
            nameof(canopyTextureName));
    }

    public Vector3d Origin { get; }

    public double OverallHeight { get; }

    public double TrunkWidth { get; }

    public double CanopyWidth { get; }

    public double CanopyHeight { get; }

    public int CanopyLayerCount { get; }

    public GenerationSeed GenerationSeed { get; }

    public GridSpacing GridSpacing { get; }

    public string TrunkTextureName { get; }

    public string CanopyTextureName { get; }

    public static TreeGenerationSettings CreateDefault(
        BrushForgeProjectSettings projectSettings)
    {
        ArgumentNullException.ThrowIfNull(projectSettings);

        double grid =
            projectSettings.GridSpacing.Units;

        return new TreeGenerationSettings(
            Vector3d.Zero,
            Math.Max(256.0, grid * 32.0),
            Math.Max(32.0, grid * 4.0),
            Math.Max(160.0, grid * 20.0),
            Math.Max(128.0, grid * 16.0),
            3,
            projectSettings.GenerationSeed,
            projectSettings.GridSpacing,
            "WOOD",
            "LEAF");
    }

    public TreeGenerationSettings WithOrigin(
        Vector3d origin)
    {
        return new TreeGenerationSettings(
            origin,
            OverallHeight,
            TrunkWidth,
            CanopyWidth,
            CanopyHeight,
            CanopyLayerCount,
            GenerationSeed,
            GridSpacing,
            TrunkTextureName,
            CanopyTextureName);
    }

    public TreeGenerationSettings WithDimensions(
        double overallHeight,
        double trunkWidth,
        double canopyWidth,
        double canopyHeight,
        int canopyLayerCount)
    {
        return new TreeGenerationSettings(
            Origin,
            overallHeight,
            trunkWidth,
            canopyWidth,
            canopyHeight,
            canopyLayerCount,
            GenerationSeed,
            GridSpacing,
            TrunkTextureName,
            CanopyTextureName);
    }

    public TreeGenerationSettings WithGenerationSeed(
        GenerationSeed generationSeed)
    {
        return new TreeGenerationSettings(
            Origin,
            OverallHeight,
            TrunkWidth,
            CanopyWidth,
            CanopyHeight,
            CanopyLayerCount,
            generationSeed,
            GridSpacing,
            TrunkTextureName,
            CanopyTextureName);
    }

    public TreeGenerationSettings WithTextures(
        string trunkTextureName,
        string canopyTextureName)
    {
        return new TreeGenerationSettings(
            Origin,
            OverallHeight,
            TrunkWidth,
            CanopyWidth,
            CanopyHeight,
            CanopyLayerCount,
            GenerationSeed,
            GridSpacing,
            trunkTextureName,
            canopyTextureName);
    }

    private static void ValidateDimension(
        double value,
        string parameterName)
    {
        if (
            !NumericTolerances.IsFinite(value) ||
            value <= 0.0 ||
            value > MaximumDimension
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Tree dimensions must be finite, greater than zero, and no greater than {MaximumDimension}.");
        }
    }

    private static void ValidateGridRepresentableDimension(
        double value,
        GridSpacing gridSpacing,
        string parameterName)
    {
        double gridUnits =
            value /
            gridSpacing.Units;

        if (
            !NumericTolerances.IsFinite(gridUnits) ||
            gridUnits > MaximumGridUnitCount
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Tree dimensions cannot exceed {MaximumGridUnitCount} grid units.");
        }
    }

    private static string NormalizeTextureName(
        string textureName,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(textureName);

        string normalized =
            textureName.Trim();

        if (
            normalized.Length == 0 ||
            normalized.Length > BrushFace.MaximumTextureNameLength
        ) {
            throw new ArgumentException(
                $"Texture names must contain between 1 and {BrushFace.MaximumTextureNameLength} characters.",
                parameterName);
        }

        if (
            normalized.Length == 1 &&
            normalized[0] == '{'
        ) {
            throw new ArgumentException(
                "A transparent texture name requires characters after its leading brace.",
                parameterName);
        }

        for (
            int index = 0;
            index < normalized.Length;
            index++
        ) {
            char character =
                normalized[index];

            bool invalidCharacter =
                char.IsWhiteSpace(character) ||
                char.IsControl(character) ||
                character == '"' ||
                character == '}' ||
                (
                    character == '{' &&
                    index != 0
                );

            if (invalidCharacter) {
                throw new ArgumentException(
                    "Texture names cannot contain whitespace, control characters, quotes, closing braces, or non-leading opening braces.",
                    parameterName);
            }
        }

        return normalized;
    }
}
