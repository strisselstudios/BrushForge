using BrushForge.Core.Randomness;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Branches;
using BrushForge.ProjectModel.Projects;

namespace BrushForge.Tests.Generation;

public sealed class TreeBranchSkeletonPlannerTests
{
    [Fact]
    public void CreateIsDeterministicForSameSeed()
    {
        TreeGenerationSettings settings =
            CreateSettings(
                seedValue: 912_345UL,
                detail: 1.0);

        TreeBranchSkeleton first =
            TreeBranchSkeletonPlanner.Create(settings);
        TreeBranchSkeleton second =
            TreeBranchSkeletonPlanner.Create(settings);

        Assert.Equal(
            CreateSignature(first),
            CreateSignature(second));
    }

    [Fact]
    public void CreateChangesNormalizedStructureForDifferentSeeds()
    {
        TreeBranchSkeleton first =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 111UL,
                    detail: 1.0));
        TreeBranchSkeleton second =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 222UL,
                    detail: 1.0));

        Assert.NotEqual(
            CreateSignature(first),
            CreateSignature(second));
    }

    [Fact]
    public void CreatePreservesSkeletonWhenOnlyDetailChanges()
    {
        TreeGenerationSettings lowDetail =
            CreateSettings(
                seedValue: 5_555UL,
                detail: 0.0);
        TreeGenerationSettings highDetail =
            lowDetail.WithDetail(1.0);

        TreeBranchSkeleton lowSkeleton =
            TreeBranchSkeletonPlanner.Create(lowDetail);
        TreeBranchSkeleton highSkeleton =
            TreeBranchSkeletonPlanner.Create(highDetail);

        Assert.Equal(
            CreateSignature(lowSkeleton),
            CreateSignature(highSkeleton));
    }

    [Fact]
    public void CreateBuildsExpectedPrimarySecondaryTertiaryAndTerminalHierarchy()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 8080UL,
                    detail: 1.0));

        int secondaryCount =
            TreeBranchSkeletonPlanner.PrimaryBranchCount *
            TreeBranchSkeletonPlanner.SecondaryBranchesPerPrimary;
        int tertiaryCount =
            secondaryCount *
            TreeBranchSkeletonPlanner.TertiaryBranchesPerSecondary;
        int terminalBranchletCount =
            tertiaryCount *
            TreeBranchSkeletonPlanner.TerminalBranchletsPerTertiary;
        int expectedCount =
            TreeBranchSkeletonPlanner.PrimaryBranchCount +
            secondaryCount +
            tertiaryCount +
            terminalBranchletCount;

        Assert.Equal(expectedCount, skeleton.Count);
        Assert.Equal(3, skeleton.MaximumDepth);
        Assert.Equal(
            TreeBranchSkeletonPlanner.PrimaryBranchCount,
            skeleton.Branches.Count(
                branch =>
                    branch.Depth == 0));
        Assert.Equal(
            secondaryCount,
            skeleton.Branches.Count(
                branch =>
                    branch.Depth == 1));
        Assert.Equal(
            tertiaryCount,
            skeleton.Branches.Count(
                branch =>
                    branch.Depth == 2));
        Assert.Equal(
            terminalBranchletCount,
            skeleton.Branches.Count(
                branch =>
                    branch.Depth == 3));
    }

    [Fact]
    public void CreateSeparatesSecondarySiblingAttachmentsAndSides()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 24_680UL,
                    detail: 1.0));

        foreach (
            PlannedTreeBranch primary in
            skeleton.Branches.Where(
                branch =>
                    branch.Depth == 0)
        ) {
            PlannedTreeBranch lowerChild =
                Assert.Single(
                    skeleton.Branches,
                    branch =>
                        branch.ParentPath == primary.Path &&
                        branch.Path.EndsWith(
                            "/S0",
                            StringComparison.Ordinal));
            PlannedTreeBranch upperChild =
                Assert.Single(
                    skeleton.Branches,
                    branch =>
                        branch.ParentPath == primary.Path &&
                        branch.Path.EndsWith(
                            "/S1",
                            StringComparison.Ordinal));

            Assert.InRange(
                lowerChild.AttachmentFraction,
                0.46,
                0.62);
            Assert.InRange(
                upperChild.AttachmentFraction,
                0.70,
                0.86);
            Assert.True(
                upperChild.AttachmentFraction -
                lowerChild.AttachmentFraction >=
                0.08);
            Assert.InRange(
                lowerChild.AzimuthDegrees,
                -78.0,
                -38.0);
            Assert.InRange(
                upperChild.AzimuthDegrees,
                38.0,
                78.0);
            Assert.InRange(
                lowerChild.ElevationDegrees,
                18.0,
                44.0);
            Assert.InRange(
                upperChild.ElevationDegrees,
                18.0,
                44.0);
        }
    }

    [Fact]
    public void CreateUsesUniqueStableBranchPaths()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 31337UL,
                    detail: 1.0));

        string[] paths =
            skeleton.Branches
                .Select(
                    branch =>
                        branch.Path)
                .ToArray();

        Assert.Equal(
            paths.Length,
            paths.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("P0", paths);
        Assert.Contains("P0/S0", paths);
        Assert.Contains("P0/S0/T0", paths);
        Assert.Contains("P4/S1", paths);
        Assert.Contains("P4/S1/T1", paths);
    }

    [Fact]
    public void CreateKeepsParentBeforeEveryChild()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 98_765UL,
                    detail: 1.0));
        Dictionary<string, int> indices =
            skeleton.Branches
                .Select(
                    (branch, index) =>
                        new KeyValuePair<string, int>(
                            branch.Path,
                            index))
                .ToDictionary(
                    pair =>
                        pair.Key,
                    pair =>
                        pair.Value,
                    StringComparer.Ordinal);

        foreach (
            PlannedTreeBranch branch in
            skeleton.Branches.Where(
                branch =>
                    branch.ParentPath is not null)
        ) {
            Assert.True(
                indices[branch.ParentPath!] <
                indices[branch.Path]);
        }
    }

    [Fact]
    public void CreateKeepsNormalizedStructuralValuesInSupportedRanges()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 1_234_567UL,
                    detail: 1.0));

        Assert.All(
            skeleton.Branches,
            branch =>
            {
                Assert.InRange(branch.AttachmentFraction, 0.0, 1.0);
                Assert.InRange(branch.AzimuthDegrees, -180.0, Math.BitDecrement(180.0));
                Assert.InRange(branch.ElevationDegrees, -90.0, 90.0);
                Assert.InRange(branch.LengthScale, Math.BitIncrement(0.0), 1.0);
                Assert.InRange(branch.StartRadiusScale, Math.BitIncrement(0.0), 1.0);
                Assert.InRange(branch.EndRadiusScale, Math.BitIncrement(0.0), branch.StartRadiusScale);
                Assert.InRange(branch.RequiredDetail, 0.0, 1.0);
            });
    }

    [Fact]
    public void CreateMakesPrimaryBranchesAvailableAtLowestDetail()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 789UL,
                    detail: 1.0));

        Assert.All(
            skeleton.Branches.Where(
                branch =>
                    branch.Depth == 0),
            branch =>
                Assert.Equal(0.0, branch.RequiredDetail));
    }

    [Fact]
    public void CreateAssignsSecondaryBranchesStableHigherDetailThresholds()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 456UL,
                    detail: 1.0));

        Assert.All(
            skeleton.Branches.Where(
                branch =>
                    branch.Depth == 1),
            branch =>
                Assert.InRange(
                    branch.RequiredDetail,
                    0.30,
                    Math.BitDecrement(0.76)));
    }

    [Fact]
    public void CreateAssignsTwoTertiariesToEverySecondaryBranch()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 45_678UL,
                    detail: 1.0));

        foreach (
            PlannedTreeBranch secondary in
            skeleton.Branches.Where(
                branch =>
                    branch.Depth == 1)
        ) {
            PlannedTreeBranch[] children =
                skeleton.Branches
                    .Where(
                        branch =>
                            string.Equals(
                                branch.ParentPath,
                                secondary.Path,
                                StringComparison.Ordinal))
                    .OrderBy(
                        branch =>
                            branch.Path,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                TreeBranchSkeletonPlanner.TertiaryBranchesPerSecondary,
                children.Length);
            Assert.All(
                children,
                child =>
                    Assert.Equal(2, child.Depth));
            Assert.EndsWith(
                "/T0",
                children[0].Path,
                StringComparison.Ordinal);
            Assert.EndsWith(
                "/T1",
                children[1].Path,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CreateKeepsTertiaryDetailThresholdAboveItsParent()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 91_827UL,
                    detail: 1.0));
        Dictionary<string, PlannedTreeBranch> branchesByPath =
            skeleton.Branches
                .ToDictionary(
                    branch =>
                        branch.Path,
                    StringComparer.Ordinal);

        Assert.All(
            skeleton.Branches.Where(
                branch =>
                    branch.Depth == 2),
            branch =>
            {
                PlannedTreeBranch parent =
                    branchesByPath[branch.ParentPath!];

                Assert.True(
                    branch.RequiredDetail >
                    parent.RequiredDetail);
                Assert.True(
                    branch.RequiredDetail < 1.0);
            });
    }

    [Fact]
    public void CreateSeparatesTertiarySiblingAttachmentsAndSides()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 62_415UL,
                    detail: 1.0));

        foreach (
            PlannedTreeBranch secondary in
            skeleton.Branches.Where(
                branch =>
                    branch.Depth == 1)
        ) {
            PlannedTreeBranch lowerChild =
                Assert.Single(
                    skeleton.Branches,
                    branch =>
                        branch.ParentPath == secondary.Path &&
                        branch.Path.EndsWith(
                            "/T0",
                            StringComparison.Ordinal));
            PlannedTreeBranch upperChild =
                Assert.Single(
                    skeleton.Branches,
                    branch =>
                        branch.ParentPath == secondary.Path &&
                        branch.Path.EndsWith(
                            "/T1",
                            StringComparison.Ordinal));

            Assert.InRange(
                lowerChild.AttachmentFraction,
                0.42,
                Math.BitDecrement(0.58));
            Assert.InRange(
                upperChild.AttachmentFraction,
                0.66,
                Math.BitDecrement(0.82));
            Assert.True(
                upperChild.AttachmentFraction >
                lowerChild.AttachmentFraction);
            Assert.InRange(
                lowerChild.AzimuthDegrees,
                -82.0,
                -42.0);
            Assert.InRange(
                upperChild.AzimuthDegrees,
                42.0,
                82.0);
            Assert.InRange(
                lowerChild.ElevationDegrees,
                20.0,
                Math.BitDecrement(46.0));
            Assert.InRange(
                upperChild.ElevationDegrees,
                20.0,
                Math.BitDecrement(46.0));
            Assert.InRange(
                lowerChild.RequiredDetail,
                0.85,
                Math.BitDecrement(0.92));
            Assert.InRange(
                upperChild.RequiredDetail,
                0.92,
                Math.BitDecrement(0.98));
        }
    }

    [Fact]
    public void CreateAssignsTwoTerminalBranchletsToEveryTertiaryBranch()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 73_951UL,
                    detail: 1.0));

        foreach (
            PlannedTreeBranch tertiary in
            skeleton.Branches.Where(
                branch =>
                    branch.Depth == 2)
        ) {
            PlannedTreeBranch[] children =
                skeleton.Branches
                    .Where(
                        branch =>
                            string.Equals(
                                branch.ParentPath,
                                tertiary.Path,
                                StringComparison.Ordinal))
                    .OrderBy(
                        branch =>
                            branch.Path,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                TreeBranchSkeletonPlanner.TerminalBranchletsPerTertiary,
                children.Length);
            Assert.All(
                children,
                child =>
                    Assert.Equal(3, child.Depth));
            Assert.EndsWith(
                "/B0",
                children[0].Path,
                StringComparison.Ordinal);
            Assert.EndsWith(
                "/B1",
                children[1].Path,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CreateKeepsTerminalBranchletDetailThresholdAboveItsParent()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 82_614UL,
                    detail: 1.0));
        Dictionary<string, PlannedTreeBranch> branchesByPath =
            skeleton.Branches
                .ToDictionary(
                    branch =>
                        branch.Path,
                    StringComparer.Ordinal);

        Assert.All(
            skeleton.Branches.Where(
                branch =>
                    branch.Depth == 3),
            branch =>
            {
                PlannedTreeBranch parent =
                    branchesByPath[branch.ParentPath!];

                Assert.True(
                    branch.RequiredDetail >
                    parent.RequiredDetail);
                Assert.InRange(
                    branch.RequiredDetail,
                    0.94,
                    Math.BitDecrement(1.0));
            });
    }

    [Fact]
    public void CreateSeparatesTerminalBranchletSiblingAttachmentsAndSides()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 31_407UL,
                    detail: 1.0));

        foreach (
            PlannedTreeBranch tertiary in
            skeleton.Branches.Where(
                branch =>
                    branch.Depth == 2)
        ) {
            PlannedTreeBranch lowerChild =
                Assert.Single(
                    skeleton.Branches,
                    branch =>
                        branch.ParentPath == tertiary.Path &&
                        branch.Path.EndsWith(
                            "/B0",
                            StringComparison.Ordinal));
            PlannedTreeBranch upperChild =
                Assert.Single(
                    skeleton.Branches,
                    branch =>
                        branch.ParentPath == tertiary.Path &&
                        branch.Path.EndsWith(
                            "/B1",
                            StringComparison.Ordinal));

            Assert.InRange(
                lowerChild.AttachmentFraction,
                0.58,
                Math.BitDecrement(0.72));
            Assert.InRange(
                upperChild.AttachmentFraction,
                0.78,
                Math.BitDecrement(0.92));
            Assert.True(
                upperChild.AttachmentFraction >
                lowerChild.AttachmentFraction);
            Assert.InRange(
                lowerChild.AzimuthDegrees,
                -88.0,
                -48.0);
            Assert.InRange(
                upperChild.AzimuthDegrees,
                48.0,
                88.0);
            Assert.InRange(
                lowerChild.ElevationDegrees,
                22.0,
                Math.BitDecrement(44.0));
            Assert.InRange(
                upperChild.ElevationDegrees,
                22.0,
                Math.BitDecrement(44.0));
        }
    }

    [Fact]
    public void SkeletonRejectsDuplicateStablePaths()
    {
        PlannedTreeBranch first =
            CreateBranch(
                path: "P0",
                parentPath: null,
                depth: 0);
        PlannedTreeBranch duplicate =
            CreateBranch(
                path: "P0",
                parentPath: null,
                depth: 0);

        Assert.Throws<ArgumentException>(
            () =>
                new TreeBranchSkeleton(
                [
                    first,
                    duplicate
                ]));
    }

    [Fact]
    public void SkeletonRejectsChildBeforeParent()
    {
        PlannedTreeBranch child =
            CreateBranch(
                path: "P0/S0",
                parentPath: "P0",
                depth: 1);
        PlannedTreeBranch parent =
            CreateBranch(
                path: "P0",
                parentPath: null,
                depth: 0);

        Assert.Throws<ArgumentException>(
            () =>
                new TreeBranchSkeleton(
                [
                    child,
                    parent
                ]));
    }

    [Fact]
    public void PlannedBranchRejectsRadiusGrowthTowardTip()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new PlannedTreeBranch(
                    "P0",
                    parentPath: null,
                    depth: 0,
                    attachmentFraction: 0.5,
                    azimuthDegrees: 0.0,
                    elevationDegrees: 30.0,
                    lengthScale: 0.5,
                    startRadiusScale: 0.25,
                    endRadiusScale: 0.5,
                    requiredDetail: 0.0));
    }

    [Fact]
    public void PlannedBranchRejectsRequiredDetailOutsideUnitInterval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new PlannedTreeBranch(
                    "P0",
                    parentPath: null,
                    depth: 0,
                    attachmentFraction: 0.5,
                    azimuthDegrees: 0.0,
                    elevationDegrees: 30.0,
                    lengthScale: 0.5,
                    startRadiusScale: 0.5,
                    endRadiusScale: 0.25,
                    requiredDetail: 1.01));
    }

    [Fact]
    public void CreateDoesNotConsumeOrMutateStoredTreeSettings()
    {
        TreeGenerationSettings settings =
            CreateSettings(
                seedValue: 42UL,
                detail: 0.375);
        GenerationSeed originalSeed =
            settings.GenerationSeed;
        double originalDetail =
            settings.Detail;

        _ = TreeBranchSkeletonPlanner.Create(settings);

        Assert.Equal(originalSeed, settings.GenerationSeed);
        Assert.Equal(originalDetail, settings.Detail);
    }

    private static PlannedTreeBranch CreateBranch(
        string path,
        string? parentPath,
        int depth)
    {
        return new PlannedTreeBranch(
            path,
            parentPath,
            depth,
            attachmentFraction: 0.5,
            azimuthDegrees: 0.0,
            elevationDegrees: 30.0,
            lengthScale: 0.5,
            startRadiusScale: 0.5,
            endRadiusScale: 0.25,
            requiredDetail:
                depth == 0
                    ? 0.0
                    : 0.5);
    }

    private static string CreateSignature(
        TreeBranchSkeleton skeleton)
    {
        return string.Join(
            "\n",
            skeleton.Branches.Select(
                branch =>
                    FormattableString.Invariant(
                        $"{branch.Path}|{branch.ParentPath ?? "<root>"}|{branch.Depth}|{branch.AttachmentFraction:R}|{branch.AzimuthDegrees:R}|{branch.ElevationDegrees:R}|{branch.LengthScale:R}|{branch.StartRadiusScale:R}|{branch.EndRadiusScale:R}|{branch.RequiredDetail:R}")));
    }

    private static TreeGenerationSettings CreateSettings(
        ulong seedValue,
        double detail)
    {
        BrushForgeProjectSettings projectSettings =
            BrushForgeProjectSettings.CreateDefault(
                new GenerationSeed(seedValue));

        return TreeGenerationSettings
            .CreateDefault(projectSettings)
            .WithDetail(detail);
    }
}
