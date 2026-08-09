using BrushForge.Generation.Foliage.Branches;

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

    public static double ResolveTrunkIrregularity(
        TreeGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.TrunkIrregularity * settings.Detail;
    }

    public static double ResolveTrunkTwist(
        TreeGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.TrunkTwist * settings.Detail;
    }

    public static int ResolveTrunkSegmentCount(
        TreeGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        int minimumSegmentCount =
            TreeGenerationSettings.MinimumTrunkSegmentCount;
        int adjustableSegmentCount =
            settings.TrunkSegmentCount -
            minimumSegmentCount;
        int realizedAdditionalSegments =
            (int)Math.Round(
                adjustableSegmentCount *
                settings.Detail,
                MidpointRounding.AwayFromZero);

        return Math.Clamp(
            minimumSegmentCount +
            realizedAdditionalSegments,
            minimumSegmentCount,
            settings.TrunkSegmentCount);
    }


    public static int ResolveBranchMaximumSegmentCount(
        PlannedTreeBranch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);

        return branch.Depth == 0
            ? 3
            : 2;
    }

    public static int ResolveBranchSegmentCount(
        TreeGenerationSettings settings,
        PlannedTreeBranch branch)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(branch);

        if (settings.Detail < branch.RequiredDetail) {
            return 0;
        }

        int maximumSegmentCount =
            ResolveBranchMaximumSegmentCount(
                branch);
        double availableDetailRange =
            TreeGenerationSettings.MaximumDetail -
            branch.RequiredDetail;

        if (availableDetailRange <= 0.0) {
            return 1;
        }

        double localDetail = Math.Clamp(
            (settings.Detail - branch.RequiredDetail) /
            availableDetailRange,
            0.0,
            1.0);
        int realizedSegmentCount =
            1 +
            (int)Math.Floor(
                localDetail *
                maximumSegmentCount);

        return Math.Clamp(
            realizedSegmentCount,
            1,
            maximumSegmentCount);
    }

    public static int ResolveCanopyLayerCount(
        TreeGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        int minimumLayerCount =
            TreeGenerationSettings.MinimumCanopyLayerCount;
        int adjustableLayerCount =
            settings.CanopyLayerCount -
            minimumLayerCount;
        int realizedAdditionalLayers =
            (int)Math.Round(
                adjustableLayerCount *
                settings.Detail,
                MidpointRounding.AwayFromZero);

        return Math.Clamp(
            minimumLayerCount +
            realizedAdditionalLayers,
            minimumLayerCount,
            settings.CanopyLayerCount);
    }
}
