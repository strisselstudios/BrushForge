using System.Globalization;
using BrushForge.Core.Randomness;

namespace BrushForge.Generation.Foliage.Branches;

/// <summary>
/// Creates the stable normalized branch hierarchy for the current baseline
/// tree archetype. Detail is deliberately not consumed here; it controls later
/// realization of this same skeleton rather than rerolling tree identity.
/// </summary>
public static class TreeBranchSkeletonPlanner
{
    public const int PrimaryBranchCount = 5;
    public const int SecondaryBranchesPerPrimary = 2;

    private const ulong BranchRandomSalt =
        0x4252414E43484647UL;

    public static TreeBranchSkeleton Create(
        TreeGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        DeterministicRandom random =
            new(
                new GenerationSeed(
                    settings.GenerationSeed.Value ^
                    BranchRandomSalt));
        List<PlannedTreeBranch> branches =
            new(
                PrimaryBranchCount *
                (SecondaryBranchesPerPrimary + 1));

        for (
            int primaryIndex = 0;
            primaryIndex < PrimaryBranchCount;
            primaryIndex++
        ) {
            string primaryPath =
                CreatePrimaryPath(primaryIndex);
            double primaryAzimuth =
                NormalizeDegrees(
                    -180.0 +
                    ((360.0 / PrimaryBranchCount) * primaryIndex) +
                    random.NextDouble(-22.0, 22.0));

            branches.Add(
                new PlannedTreeBranch(
                    primaryPath,
                    parentPath: null,
                    depth: 0,
                    attachmentFraction:
                        random.NextDouble(0.34, 0.84),
                    azimuthDegrees: primaryAzimuth,
                    elevationDegrees:
                        random.NextDouble(18.0, 52.0),
                    lengthScale:
                        random.NextDouble(0.46, 0.74),
                    startRadiusScale:
                        random.NextDouble(0.34, 0.48),
                    endRadiusScale:
                        random.NextDouble(0.14, 0.24),
                    requiredDetail: 0.0));

            for (
                int secondaryIndex = 0;
                secondaryIndex < SecondaryBranchesPerPrimary;
                secondaryIndex++
            ) {
                double secondaryStartRadiusScale =
                    random.NextDouble(0.52, 0.72);

                branches.Add(
                    new PlannedTreeBranch(
                        CreateChildPath(
                            primaryPath,
                            secondaryIndex),
                        primaryPath,
                        depth: 1,
                        attachmentFraction:
                            random.NextDouble(0.48, 0.86),
                        azimuthDegrees:
                            random.NextDouble(-82.0, 82.0),
                        elevationDegrees:
                            random.NextDouble(12.0, 58.0),
                        lengthScale:
                            random.NextDouble(0.42, 0.68),
                        startRadiusScale:
                            secondaryStartRadiusScale,
                        endRadiusScale:
                            secondaryStartRadiusScale *
                            random.NextDouble(0.42, 0.62),
                        requiredDetail:
                            random.NextDouble(0.30, 0.76)));
            }
        }

        return new TreeBranchSkeleton(branches);
    }

    private static string CreatePrimaryPath(
        int primaryIndex)
    {
        return string.Concat(
            "P",
            primaryIndex.ToString(
                CultureInfo.InvariantCulture));
    }

    private static string CreateChildPath(
        string parentPath,
        int childIndex)
    {
        return string.Concat(
            parentPath,
            "/S",
            childIndex.ToString(
                CultureInfo.InvariantCulture));
    }

    private static double NormalizeDegrees(
        double degrees)
    {
        double normalized =
            degrees % 360.0;

        if (normalized < -180.0) {
            normalized += 360.0;
        }
        else if (normalized >= 180.0) {
            normalized -= 360.0;
        }

        return normalized;
    }
}
