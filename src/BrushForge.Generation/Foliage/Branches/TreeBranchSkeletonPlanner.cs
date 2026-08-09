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
    private const double LowerSecondaryAttachmentMinimum = 0.46;
    private const double LowerSecondaryAttachmentMaximum = 0.62;
    private const double UpperSecondaryAttachmentMinimum = 0.70;
    private const double UpperSecondaryAttachmentMaximum = 0.86;
    private const double SecondaryAzimuthMinimumMagnitude = 38.0;
    private const double SecondaryAzimuthMaximumMagnitude = 78.0;
    private const double SecondaryDeflectionMinimum = 18.0;
    private const double SecondaryDeflectionMaximum = 44.0;

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
                            CreateSecondaryAttachmentFraction(
                                random,
                                secondaryIndex),
                        azimuthDegrees:
                            CreateSecondaryAzimuthDegrees(
                                random,
                                secondaryIndex),
                        elevationDegrees:
                            random.NextDouble(
                                SecondaryDeflectionMinimum,
                                SecondaryDeflectionMaximum),
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

    private static double CreateSecondaryAttachmentFraction(
        DeterministicRandom random,
        int secondaryIndex)
    {
        return secondaryIndex switch
        {
            0 => random.NextDouble(
                LowerSecondaryAttachmentMinimum,
                LowerSecondaryAttachmentMaximum),
            1 => random.NextDouble(
                UpperSecondaryAttachmentMinimum,
                UpperSecondaryAttachmentMaximum),
            _ => throw new ArgumentOutOfRangeException(
                nameof(secondaryIndex),
                secondaryIndex,
                "The current generic tree archetype defines exactly two secondary branches per primary branch.")
        };
    }

    private static double CreateSecondaryAzimuthDegrees(
        DeterministicRandom random,
        int secondaryIndex)
    {
        double magnitude =
            random.NextDouble(
                SecondaryAzimuthMinimumMagnitude,
                SecondaryAzimuthMaximumMagnitude);

        return secondaryIndex switch
        {
            0 => -magnitude,
            1 => magnitude,
            _ => throw new ArgumentOutOfRangeException(
                nameof(secondaryIndex),
                secondaryIndex,
                "The current generic tree archetype defines exactly two secondary branches per primary branch.")
        };
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
