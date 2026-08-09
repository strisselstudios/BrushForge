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
    public void CreateBuildsExpectedPrimaryAndSecondaryHierarchy()
    {
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                CreateSettings(
                    seedValue: 8080UL,
                    detail: 1.0));

        int expectedCount =
            TreeBranchSkeletonPlanner.PrimaryBranchCount *
            (TreeBranchSkeletonPlanner.SecondaryBranchesPerPrimary + 1);

        Assert.Equal(expectedCount, skeleton.Count);
        Assert.Equal(1, skeleton.MaximumDepth);
        Assert.Equal(
            TreeBranchSkeletonPlanner.PrimaryBranchCount,
            skeleton.Branches.Count(
                branch =>
                    branch.Depth == 0));
        Assert.Equal(
            TreeBranchSkeletonPlanner.PrimaryBranchCount *
            TreeBranchSkeletonPlanner.SecondaryBranchesPerPrimary,
            skeleton.Branches.Count(
                branch =>
                    branch.Depth == 1));
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
        Assert.Contains("P4/S1", paths);
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
