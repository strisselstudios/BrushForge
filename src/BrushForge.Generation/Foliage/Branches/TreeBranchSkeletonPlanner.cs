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
    public const int TertiaryBranchesPerSecondary = 2;
    public const int TerminalBranchletsPerTertiary = 2;

    private const ulong BranchRandomSalt =
        0x4252414E43484647UL;
    private const ulong TertiaryBranchRandomSalt =
        0x5445525449415259UL;
    private const ulong TerminalBranchletRandomSalt =
        0x4252414E43484C54UL;
    private const double LowerSecondaryAttachmentMinimum = 0.46;
    private const double LowerSecondaryAttachmentMaximum = 0.62;
    private const double UpperSecondaryAttachmentMinimum = 0.70;
    private const double UpperSecondaryAttachmentMaximum = 0.86;
    private const double SecondaryAzimuthMinimumMagnitude = 38.0;
    private const double SecondaryAzimuthMaximumMagnitude = 78.0;
    private const double SecondaryDeflectionMinimum = 18.0;
    private const double SecondaryDeflectionMaximum = 44.0;
    private const double LowerTertiaryAttachmentMinimum = 0.42;
    private const double LowerTertiaryAttachmentMaximum = 0.58;
    private const double UpperTertiaryAttachmentMinimum = 0.66;
    private const double UpperTertiaryAttachmentMaximum = 0.82;
    private const double TertiaryAzimuthMinimumMagnitude = 42.0;
    private const double TertiaryAzimuthMaximumMagnitude = 82.0;
    private const double TertiaryDeflectionMinimum = 20.0;
    private const double TertiaryDeflectionMaximum = 46.0;
    private const double LowerTertiaryRequiredDetailMinimum = 0.85;
    private const double LowerTertiaryRequiredDetailMaximum = 0.92;
    private const double UpperTertiaryRequiredDetailMinimum = 0.92;
    private const double UpperTertiaryRequiredDetailMaximum = 0.98;
    private const double LowerTerminalAttachmentMinimum = 0.58;
    private const double LowerTerminalAttachmentMaximum = 0.72;
    private const double UpperTerminalAttachmentMinimum = 0.78;
    private const double UpperTerminalAttachmentMaximum = 0.92;
    private const double TerminalAzimuthMinimumMagnitude = 48.0;
    private const double TerminalAzimuthMaximumMagnitude = 88.0;
    private const double TerminalDeflectionMinimum = 22.0;
    private const double TerminalDeflectionMaximum = 44.0;
    private const double LowerTerminalRequiredDetailMinimum = 0.94;
    private const double LowerTerminalRequiredDetailMaximum = 0.975;
    private const double UpperTerminalRequiredDetailMinimum = 0.975;
    private const double UpperTerminalRequiredDetailMaximum = 0.995;
    private const double TerminalRequiredDetailParentMargin = 0.01;
    private const double MaximumTerminalRequiredDetail = 0.999;

    public static TreeBranchSkeleton Create(
        TreeGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        DeterministicRandom random =
            new(
                new GenerationSeed(
                    settings.GenerationSeed.Value ^
                    BranchRandomSalt));
        int secondaryCount =
            PrimaryBranchCount *
            SecondaryBranchesPerPrimary;
        int tertiaryCount =
            secondaryCount *
            TertiaryBranchesPerSecondary;
        int terminalBranchletCount =
            tertiaryCount *
            TerminalBranchletsPerTertiary;
        List<PlannedTreeBranch> branches =
            new(
                PrimaryBranchCount +
                secondaryCount +
                tertiaryCount +
                terminalBranchletCount);

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

        PlannedTreeBranch[] secondaryBranches =
            branches
                .Where(
                    branch =>
                        branch.Depth == 1)
                .ToArray();

        foreach (PlannedTreeBranch secondary in secondaryBranches) {
            for (
                int tertiaryIndex = 0;
                tertiaryIndex < TertiaryBranchesPerSecondary;
                tertiaryIndex++
            ) {
                string tertiaryPath =
                    CreateTertiaryPath(
                        secondary.Path,
                        tertiaryIndex);
                DeterministicRandom tertiaryRandom =
                    CreateTertiaryRandom(
                        settings.GenerationSeed,
                        tertiaryPath);
                double tertiaryStartRadiusScale =
                    tertiaryRandom.NextDouble(0.38, 0.58);

                branches.Add(
                    new PlannedTreeBranch(
                        tertiaryPath,
                        secondary.Path,
                        depth: 2,
                        attachmentFraction:
                            CreateTertiaryAttachmentFraction(
                                tertiaryRandom,
                                tertiaryIndex),
                        azimuthDegrees:
                            CreateTertiaryAzimuthDegrees(
                                tertiaryRandom,
                                tertiaryIndex),
                        elevationDegrees:
                            tertiaryRandom.NextDouble(
                                TertiaryDeflectionMinimum,
                                TertiaryDeflectionMaximum),
                        lengthScale:
                            tertiaryRandom.NextDouble(0.32, 0.52),
                        startRadiusScale:
                            tertiaryStartRadiusScale,
                        endRadiusScale:
                            tertiaryStartRadiusScale *
                            tertiaryRandom.NextDouble(0.35, 0.55),
                        requiredDetail:
                            CreateTertiaryRequiredDetail(
                                tertiaryRandom,
                                tertiaryIndex)));
            }
        }

        PlannedTreeBranch[] tertiaryBranches =
            branches
                .Where(
                    branch =>
                        branch.Depth == 2)
                .ToArray();

        foreach (PlannedTreeBranch tertiary in tertiaryBranches) {
            for (
                int branchletIndex = 0;
                branchletIndex < TerminalBranchletsPerTertiary;
                branchletIndex++
            ) {
                string branchletPath =
                    CreateTerminalBranchletPath(
                        tertiary.Path,
                        branchletIndex);
                DeterministicRandom branchletRandom =
                    CreateTerminalBranchletRandom(
                        settings.GenerationSeed,
                        branchletPath);
                double branchletStartRadiusScale =
                    branchletRandom.NextDouble(0.30, 0.46);

                branches.Add(
                    new PlannedTreeBranch(
                        branchletPath,
                        tertiary.Path,
                        depth: 3,
                        attachmentFraction:
                            CreateTerminalAttachmentFraction(
                                branchletRandom,
                                branchletIndex),
                        azimuthDegrees:
                            CreateTerminalAzimuthDegrees(
                                branchletRandom,
                                branchletIndex),
                        elevationDegrees:
                            branchletRandom.NextDouble(
                                TerminalDeflectionMinimum,
                                TerminalDeflectionMaximum),
                        lengthScale:
                            branchletRandom.NextDouble(0.22, 0.38),
                        startRadiusScale:
                            branchletStartRadiusScale,
                        endRadiusScale:
                            branchletStartRadiusScale *
                            branchletRandom.NextDouble(0.28, 0.48),
                        requiredDetail:
                            CreateTerminalRequiredDetail(
                                branchletRandom,
                                branchletIndex,
                                tertiary.RequiredDetail)));
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

    private static DeterministicRandom CreateTertiaryRandom(
        GenerationSeed treeSeed,
        string tertiaryPath)
    {
        GenerationSeed pathSeed =
            GenerationSeed.FromText(
                tertiaryPath);

        return new DeterministicRandom(
            new GenerationSeed(
                treeSeed.Value ^
                TertiaryBranchRandomSalt ^
                pathSeed.Value));
    }

    private static double CreateTertiaryAttachmentFraction(
        DeterministicRandom random,
        int tertiaryIndex)
    {
        return tertiaryIndex switch
        {
            0 => random.NextDouble(
                LowerTertiaryAttachmentMinimum,
                LowerTertiaryAttachmentMaximum),
            1 => random.NextDouble(
                UpperTertiaryAttachmentMinimum,
                UpperTertiaryAttachmentMaximum),
            _ => throw new ArgumentOutOfRangeException(
                nameof(tertiaryIndex),
                tertiaryIndex,
                "The current generic tree archetype defines exactly two tertiary branches per secondary branch.")
        };
    }

    private static double CreateTertiaryAzimuthDegrees(
        DeterministicRandom random,
        int tertiaryIndex)
    {
        double magnitude =
            random.NextDouble(
                TertiaryAzimuthMinimumMagnitude,
                TertiaryAzimuthMaximumMagnitude);

        return tertiaryIndex switch
        {
            0 => -magnitude,
            1 => magnitude,
            _ => throw new ArgumentOutOfRangeException(
                nameof(tertiaryIndex),
                tertiaryIndex,
                "The current generic tree archetype defines exactly two tertiary branches per secondary branch.")
        };
    }

    private static double CreateTertiaryRequiredDetail(
        DeterministicRandom random,
        int tertiaryIndex)
    {
        return tertiaryIndex switch
        {
            0 => random.NextDouble(
                LowerTertiaryRequiredDetailMinimum,
                LowerTertiaryRequiredDetailMaximum),
            1 => random.NextDouble(
                UpperTertiaryRequiredDetailMinimum,
                UpperTertiaryRequiredDetailMaximum),
            _ => throw new ArgumentOutOfRangeException(
                nameof(tertiaryIndex),
                tertiaryIndex,
                "The current generic tree archetype defines exactly two tertiary branches per secondary branch.")
        };
    }

    private static DeterministicRandom CreateTerminalBranchletRandom(
        GenerationSeed treeSeed,
        string branchletPath)
    {
        GenerationSeed pathSeed =
            GenerationSeed.FromText(
                branchletPath);

        return new DeterministicRandom(
            new GenerationSeed(
                treeSeed.Value ^
                TerminalBranchletRandomSalt ^
                pathSeed.Value));
    }

    private static double CreateTerminalAttachmentFraction(
        DeterministicRandom random,
        int branchletIndex)
    {
        return branchletIndex switch
        {
            0 => random.NextDouble(
                LowerTerminalAttachmentMinimum,
                LowerTerminalAttachmentMaximum),
            1 => random.NextDouble(
                UpperTerminalAttachmentMinimum,
                UpperTerminalAttachmentMaximum),
            _ => throw new ArgumentOutOfRangeException(
                nameof(branchletIndex),
                branchletIndex,
                "The current generic tree archetype defines exactly two terminal branchlets per tertiary branch.")
        };
    }

    private static double CreateTerminalAzimuthDegrees(
        DeterministicRandom random,
        int branchletIndex)
    {
        double magnitude =
            random.NextDouble(
                TerminalAzimuthMinimumMagnitude,
                TerminalAzimuthMaximumMagnitude);

        return branchletIndex switch
        {
            0 => -magnitude,
            1 => magnitude,
            _ => throw new ArgumentOutOfRangeException(
                nameof(branchletIndex),
                branchletIndex,
                "The current generic tree archetype defines exactly two terminal branchlets per tertiary branch.")
        };
    }

    private static double CreateTerminalRequiredDetail(
        DeterministicRandom random,
        int branchletIndex,
        double parentRequiredDetail)
    {
        double sampledDetail =
            branchletIndex switch
            {
                0 => random.NextDouble(
                    LowerTerminalRequiredDetailMinimum,
                    LowerTerminalRequiredDetailMaximum),
                1 => random.NextDouble(
                    UpperTerminalRequiredDetailMinimum,
                    UpperTerminalRequiredDetailMaximum),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(branchletIndex),
                    branchletIndex,
                    "The current generic tree archetype defines exactly two terminal branchlets per tertiary branch.")
            };
        double minimumDetail =
            Math.Min(
                MaximumTerminalRequiredDetail,
                parentRequiredDetail +
                TerminalRequiredDetailParentMargin);

        return Math.Max(
            sampledDetail,
            minimumDetail);
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

    private static string CreateTertiaryPath(
        string parentPath,
        int childIndex)
    {
        return string.Concat(
            parentPath,
            "/T",
            childIndex.ToString(
                CultureInfo.InvariantCulture));
    }

    private static string CreateTerminalBranchletPath(
        string parentPath,
        int childIndex)
    {
        return string.Concat(
            parentPath,
            "/B",
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
