using System.Globalization;
using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Generation.Foliage.Input;

/// <summary>
/// Converts invariant user-interface text into validated tree settings.
/// </summary>
public static class TreeGenerationInputParser
{
    public static TreeGenerationSettings Parse(
        TreeGenerationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        GenerationSeed generationSeed =
            ParseGenerationSeed(
                input.GenerationSeed);

        double overallHeight =
            ParsePositiveDouble(
                input.OverallHeight,
                "Overall height");

        double trunkWidth =
            ParsePositiveDouble(
                input.TrunkWidth,
                "Trunk width");

        double canopyWidth =
            ParsePositiveDouble(
                input.CanopyWidth,
                "Canopy width");

        double canopyHeight =
            ParsePositiveDouble(
                input.CanopyHeight,
                "Canopy height");

        int canopyLayerCount =
            ParsePositiveInteger(
                input.CanopyLayerCount,
                "Canopy layer count");

        int trunkSegmentCount =
            ParsePositiveInteger(
                input.TrunkSegmentCount,
                "Trunk segment count");

        double trunkTaperPercent =
            ParseBoundedDouble(
                input.TrunkTaperPercent,
                minimum:
                    TreeGenerationSettings.MinimumTrunkTaper *
                    100.0,
                maximum:
                    TreeGenerationSettings.MaximumTrunkTaper *
                    100.0,
                displayName: "Trunk taper");

        double trunkLeanPercent =
            ParseBoundedDouble(
                input.TrunkLeanPercent,
                minimum:
                    TreeGenerationSettings.MinimumTrunkLean *
                    100.0,
                maximum:
                    TreeGenerationSettings.MaximumTrunkLean *
                    100.0,
                displayName: "Trunk lean");

        double trunkBendPercent =
            ParseBoundedDouble(
                input.TrunkBendPercent,
                minimum:
                    TreeGenerationSettings.MinimumTrunkBend *
                    100.0,
                maximum:
                    TreeGenerationSettings.MaximumTrunkBend *
                    100.0,
                displayName: "Trunk bend");

        double trunkBaseFlarePercent =
            ParseBoundedDouble(
                input.TrunkBaseFlarePercent,
                minimum:
                    TreeGenerationSettings.MinimumTrunkBaseFlare *
                    100.0,
                maximum:
                    TreeGenerationSettings.MaximumTrunkBaseFlare *
                    100.0,
                displayName: "Trunk base flare");

        TrunkCrossSectionProfile trunkCrossSection =
            ParseTrunkCrossSection(
                input.TrunkCrossSection);

        double trunkIrregularityPercent =
            ParseBoundedDouble(
                input.TrunkIrregularityPercent,
                minimum:
                    TreeGenerationSettings.MinimumTrunkIrregularity *
                    100.0,
                maximum:
                    TreeGenerationSettings.MaximumTrunkIrregularity *
                    100.0,
                displayName: "Trunk irregularity");

        double trunkTwistPercent =
            ParseBoundedDouble(
                input.TrunkTwistPercent,
                minimum:
                    TreeGenerationSettings.MinimumTrunkTwist *
                    100.0,
                maximum:
                    TreeGenerationSettings.MaximumTrunkTwist *
                    100.0,
                displayName: "Trunk twist");

        double gridUnits =
            ParsePositiveDouble(
                input.GridSpacing,
                "Grid spacing");

        return new TreeGenerationSettings(
            Vector3d.Zero,
            overallHeight,
            trunkWidth,
            canopyWidth,
            canopyHeight,
            canopyLayerCount,
            generationSeed,
            new GridSpacing(gridUnits),
            input.TrunkTextureName,
            input.CanopyTextureName,
            trunkSegmentCount,
            trunkTaperPercent / 100.0,
            trunkLeanPercent / 100.0,
            trunkBendPercent / 100.0,
            trunkBaseFlarePercent / 100.0,
            trunkCrossSection,
            trunkIrregularityPercent / 100.0,
            trunkTwistPercent / 100.0);
    }

    private static TrunkCrossSectionProfile ParseTrunkCrossSection(
        string text)
    {
        if (
            !Enum.TryParse(
                text,
                ignoreCase: true,
                out TrunkCrossSectionProfile profile) ||
            !Enum.IsDefined(profile)
        ) {
            throw new FormatException(
                "Trunk cross-section must be Square or Octagonal.");
        }

        return profile;
    }

    private static GenerationSeed ParseGenerationSeed(
        string text)
    {
        if (
            !GenerationSeed.TryParse(
                text,
                out GenerationSeed generationSeed)
        ) {
            throw new FormatException(
                "Generation seed must be an unsigned 64-bit integer.");
        }

        return generationSeed;
    }

    private static double ParsePositiveDouble(
        string text,
        string displayName)
    {
        if (
            !double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value) ||
            !double.IsFinite(value) ||
            value <= 0.0
        ) {
            throw new FormatException(
                $"{displayName} must be a finite number greater than zero using a period as the decimal separator.");
        }

        return value;
    }

    private static double ParseBoundedDouble(
        string text,
        double minimum,
        double maximum,
        string displayName)
    {
        if (
            !double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value) ||
            !double.IsFinite(value) ||
            value < minimum ||
            value > maximum
        ) {
            throw new FormatException(
                $"{displayName} must be a finite number from {minimum.ToString("0.##", CultureInfo.InvariantCulture)} through {maximum.ToString("0.##", CultureInfo.InvariantCulture)} using a period as the decimal separator.");
        }

        return value;
    }

    private static int ParsePositiveInteger(
        string text,
        string displayName)
    {
        if (
            !int.TryParse(
                text,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int value) ||
            value <= 0
        ) {
            throw new FormatException(
                $"{displayName} must be a positive whole number.");
        }

        return value;
    }
}
