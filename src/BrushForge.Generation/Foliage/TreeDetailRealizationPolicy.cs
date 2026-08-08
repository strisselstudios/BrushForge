namespace BrushForge.Generation.Foliage;

/// <summary>
/// Resolves geometry complexity from the normalized Detail setting without
/// changing the stored seed-defined tree settings.
/// </summary>
internal static class TreeDetailRealizationPolicy
{
    private const double OctagonalTrunkDetailThreshold = 0.50;

    public static TrunkCrossSectionProfile ResolveTrunkCrossSection(
        TreeGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (
            settings.TrunkCrossSection ==
                TrunkCrossSectionProfile.Octagonal &&
            settings.Detail < OctagonalTrunkDetailThreshold
        ) {
            return TrunkCrossSectionProfile.Square;
        }

        return settings.TrunkCrossSection;
    }
}
