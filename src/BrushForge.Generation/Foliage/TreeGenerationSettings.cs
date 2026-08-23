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
    public const int MinimumTrunkSegmentCount = 2;
    public const int MaximumTrunkSegmentCount = 6;
    public const int DefaultTrunkSegmentCount = 3;
    public const double MinimumTrunkTaper = 0.0;
    public const double MaximumTrunkTaper = 0.75;
    public const double DefaultTrunkTaper = 0.25;
    public const double MinimumTrunkLean = 0.0;
    public const double MaximumTrunkLean = 0.30;
    public const double DefaultTrunkLean = 0.0;
    public const double MinimumTrunkBend = 0.0;
    public const double MaximumTrunkBend = 0.20;
    public const double DefaultTrunkBend = 0.0;
    public const double MinimumTrunkBaseFlare = 0.0;
    public const double MaximumTrunkBaseFlare = 1.0;
    public const double DefaultTrunkBaseFlare = 0.0;
    public const TrunkCrossSectionProfile DefaultTrunkCrossSection =
        TrunkCrossSectionProfile.Octagonal;
    public const double MinimumTrunkIrregularity = 0.0;
    public const double MaximumTrunkIrregularity = 0.25;
    public const double DefaultTrunkIrregularity = 0.0;
    public const double MinimumTrunkTwist = 0.0;
    public const double MaximumTrunkTwist = 1.0;
    public const double DefaultTrunkTwist = 0.0;
    public const double MinimumDetail = 0.0;
    public const double MaximumDetail = 1.0;
    public const double DefaultDetail = 1.0;
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
        string canopyTextureName,
        int trunkSegmentCount = DefaultTrunkSegmentCount,
        double trunkTaper = DefaultTrunkTaper,
        double trunkLean = DefaultTrunkLean,
        double trunkBend = DefaultTrunkBend,
        double trunkBaseFlare = DefaultTrunkBaseFlare,
        TrunkCrossSectionProfile trunkCrossSection =
            DefaultTrunkCrossSection,
        double trunkIrregularity = DefaultTrunkIrregularity,
        double trunkTwist = DefaultTrunkTwist,
        double detail = DefaultDetail)
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

        if (
            trunkSegmentCount < MinimumTrunkSegmentCount ||
            trunkSegmentCount > MaximumTrunkSegmentCount
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkSegmentCount),
                trunkSegmentCount,
                $"Trunk segment count must be between {MinimumTrunkSegmentCount} and {MaximumTrunkSegmentCount}.");
        }

        if (
            !NumericTolerances.IsFinite(trunkTaper) ||
            trunkTaper < MinimumTrunkTaper ||
            trunkTaper > MaximumTrunkTaper
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkTaper),
                trunkTaper,
                $"Trunk taper must be between {MinimumTrunkTaper:P0} and {MaximumTrunkTaper:P0}.");
        }

        ValidateTrunkDeformation(
            trunkLean,
            MinimumTrunkLean,
            MaximumTrunkLean,
            nameof(trunkLean),
            "Trunk lean");

        ValidateTrunkDeformation(
            trunkBend,
            MinimumTrunkBend,
            MaximumTrunkBend,
            nameof(trunkBend),
            "Trunk bend");

        ValidateTrunkDeformation(
            trunkBaseFlare,
            MinimumTrunkBaseFlare,
            MaximumTrunkBaseFlare,
            nameof(trunkBaseFlare),
            "Trunk base flare");

        if (!Enum.IsDefined(trunkCrossSection)) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkCrossSection),
                trunkCrossSection,
                "The trunk cross-section profile is not supported.");
        }

        ValidateTrunkDeformation(
            trunkIrregularity,
            MinimumTrunkIrregularity,
            MaximumTrunkIrregularity,
            nameof(trunkIrregularity),
            "Trunk irregularity");

        ValidateTrunkDeformation(
            trunkTwist,
            MinimumTrunkTwist,
            MaximumTrunkTwist,
            nameof(trunkTwist),
            "Trunk twist");

        ValidateTrunkDeformation(
            detail,
            MinimumDetail,
            MaximumDetail,
            nameof(detail),
            "Detail");

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

        int maximumTrunkSegmentCount =
            CalculateTrunkTopUnitCount(
                overallHeight,
                canopyHeight,
                canopyLayerCount,
                gridSpacing);

        if (trunkSegmentCount > maximumTrunkSegmentCount) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkSegmentCount),
                trunkSegmentCount,
                "Trunk segment count cannot exceed the generated trunk height in grid units.");
        }

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
        TrunkSegmentCount = trunkSegmentCount;
        TrunkTaper = trunkTaper;
        TrunkLean = trunkLean;
        TrunkBend = trunkBend;
        TrunkBaseFlare = trunkBaseFlare;
        TrunkCrossSection = trunkCrossSection;
        TrunkIrregularity = trunkIrregularity;
        TrunkTwist = trunkTwist;
        Detail = detail;
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

    public int TrunkSegmentCount { get; }

    public double TrunkTaper { get; }

    public double TrunkLean { get; }

    public double TrunkBend { get; }

    public double TrunkBaseFlare { get; }

    public TrunkCrossSectionProfile TrunkCrossSection { get; }

    public double TrunkIrregularity { get; }

    public double TrunkTwist { get; }

    public double Detail { get; }

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
            CanopyTextureName,
            TrunkSegmentCount,
            TrunkTaper,
            TrunkLean,
            TrunkBend,
            TrunkBaseFlare,
            TrunkCrossSection,
            TrunkIrregularity,
            TrunkTwist,
            Detail);
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
            CanopyTextureName,
            TrunkSegmentCount,
            TrunkTaper,
            TrunkLean,
            TrunkBend,
            TrunkBaseFlare,
            TrunkCrossSection,
            TrunkIrregularity,
            TrunkTwist,
            Detail);
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
            CanopyTextureName,
            TrunkSegmentCount,
            TrunkTaper,
            TrunkLean,
            TrunkBend,
            TrunkBaseFlare,
            TrunkCrossSection,
            TrunkIrregularity,
            TrunkTwist,
            Detail);
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
            canopyTextureName,
            TrunkSegmentCount,
            TrunkTaper,
            TrunkLean,
            TrunkBend,
            TrunkBaseFlare,
            TrunkCrossSection,
            TrunkIrregularity,
            TrunkTwist,
            Detail);
    }

    public TreeGenerationSettings WithTrunkShape(
        int trunkSegmentCount,
        double trunkTaper)
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
            TrunkTextureName,
            CanopyTextureName,
            trunkSegmentCount,
            trunkTaper,
            TrunkLean,
            TrunkBend,
            TrunkBaseFlare,
            TrunkCrossSection,
            TrunkIrregularity,
            TrunkTwist,
            Detail);
    }

    public TreeGenerationSettings WithTrunkDeformation(
        double trunkLean,
        double trunkBend)
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
            TrunkTextureName,
            CanopyTextureName,
            TrunkSegmentCount,
            TrunkTaper,
            trunkLean,
            trunkBend,
            TrunkBaseFlare,
            TrunkCrossSection,
            TrunkIrregularity,
            TrunkTwist,
            Detail);
    }

    public TreeGenerationSettings WithTrunkBaseFlare(
        double trunkBaseFlare)
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
            TrunkTextureName,
            CanopyTextureName,
            TrunkSegmentCount,
            TrunkTaper,
            TrunkLean,
            TrunkBend,
            trunkBaseFlare,
            TrunkCrossSection,
            TrunkIrregularity,
            TrunkTwist,
            Detail);
    }

    public TreeGenerationSettings WithTrunkCrossSection(
        TrunkCrossSectionProfile trunkCrossSection)
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
            TrunkTextureName,
            CanopyTextureName,
            TrunkSegmentCount,
            TrunkTaper,
            TrunkLean,
            TrunkBend,
            TrunkBaseFlare,
            trunkCrossSection,
            TrunkIrregularity,
            TrunkTwist,
            Detail);
    }

    public TreeGenerationSettings WithTrunkIrregularity(
        double trunkIrregularity,
        double trunkTwist)
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
            TrunkTextureName,
            CanopyTextureName,
            TrunkSegmentCount,
            TrunkTaper,
            TrunkLean,
            TrunkBend,
            TrunkBaseFlare,
            TrunkCrossSection,
            trunkIrregularity,
            trunkTwist,
            Detail);
    }

    public TreeGenerationSettings WithDetail(
        double detail)
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
            TrunkTextureName,
            CanopyTextureName,
            TrunkSegmentCount,
            TrunkTaper,
            TrunkLean,
            TrunkBend,
            TrunkBaseFlare,
            TrunkCrossSection,
            TrunkIrregularity,
            TrunkTwist,
            detail);
    }

    private static int CalculateTrunkTopUnitCount(
        double overallHeight,
        double canopyHeight,
        int canopyLayerCount,
        GridSpacing gridSpacing)
    {
        int overallHeightUnits =
            Math.Max(
                2,
                checked(
                    (int)Math.Round(
                        overallHeight /
                        gridSpacing.Units,
                        MidpointRounding.AwayFromZero)));

        int canopyHeightUnits =
            Math.Max(
                canopyLayerCount,
                checked(
                    (int)Math.Round(
                        canopyHeight /
                        gridSpacing.Units,
                        MidpointRounding.AwayFromZero)));

        canopyHeightUnits =
            Math.Min(
                canopyHeightUnits,
                overallHeightUnits - 1);

        int canopyBottomUnits =
            overallHeightUnits -
            canopyHeightUnits;

        return Math.Min(
            overallHeightUnits,
            canopyBottomUnits + 1);
    }

    private static void ValidateTrunkDeformation(
        double value,
        double minimum,
        double maximum,
        string parameterName,
        string displayName)
    {
        if (
            !NumericTolerances.IsFinite(value) ||
            value < minimum ||
            value > maximum
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{displayName} must be between {minimum:P0} and {maximum:P0}.");
        }
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
