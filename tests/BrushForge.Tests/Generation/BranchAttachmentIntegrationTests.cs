using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Branches;

namespace BrushForge.Tests.Generation;

public sealed class BranchAttachmentIntegrationTests
{
    private const double CoordinateTolerance = 1e-8;

    [Fact]
    public void GenerateEmbedsPrimaryBranchRootsIntoStraightSquareTrunkSurface()
    {
        const double trunkWidth = 64.0;
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkWidth: trunkWidth,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkTaper: 0.0,
                    trunkSegmentCount: 6));
        GeneratedTreeBrush[] primaryRoots =
            GetPrimaryRootSegments(
                result);

        Assert.Equal(
            TreeBranchSkeletonPlanner.PrimaryBranchCount,
            primaryRoots.Length);

        foreach (GeneratedTreeBrush branch in primaryRoots) {
            Vector3d rootCenter =
                GetStartCenter(
                    branch);
            double squareSurfaceCoordinate =
                Math.Max(
                    Math.Abs(rootCenter.X),
                    Math.Abs(rootCenter.Y));
            double branchHalfExtent =
                GetStartHalfExtent(
                    branch);

            Assert.True(
                squareSurfaceCoordinate <
                (trunkWidth / 2.0));
            Assert.True(
                squareSurfaceCoordinate >=
                (trunkWidth / 2.0) -
                branchHalfExtent -
                CoordinateTolerance);
        }
    }

    [Fact]
    public void GenerateScalesPrimaryBranchThicknessSublinearlyAboveReferenceWidth()
    {
        TreeGenerationResult reference =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkWidth: 32.0,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkTaper: 0.0));
        TreeGenerationResult thick =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkWidth: 64.0,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkTaper: 0.0));
        GeneratedTreeBrush[] referenceRoots =
            GetPrimaryRootSegments(reference);
        GeneratedTreeBrush[] thickRoots =
            GetPrimaryRootSegments(thick);

        Assert.Equal(
            referenceRoots.Length,
            thickRoots.Length);

        for (
            int index = 0;
            index < referenceRoots.Length;
            index++
        ) {
            Assert.Equal(
                referenceRoots[index].BranchPath,
                thickRoots[index].BranchPath);

            double referenceHalfExtent =
                GetStartHalfExtent(
                    referenceRoots[index]);
            double thickHalfExtent =
                GetStartHalfExtent(
                    thickRoots[index]);
            double ratio =
                thickHalfExtent /
                referenceHalfExtent;

            Assert.True(
                thickHalfExtent >
                referenceHalfExtent);
            Assert.True(
                Math.Abs(
                    ratio -
                    Math.Sqrt(2.0)) <=
                CoordinateTolerance);
        }
    }

    [Fact]
    public void GenerateStillRespondsToLocalTaperedTrunkWidth()
    {
        TreeGenerationResult untapered =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 52_901UL,
                    trunkWidth: 64.0,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkTaper: 0.0,
                    trunkSegmentCount: 6));
        TreeGenerationResult tapered =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 52_901UL,
                    trunkWidth: 64.0,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkTaper: 0.50,
                    trunkSegmentCount: 6));
        GeneratedTreeBrush[] untaperedRoots =
            GetPrimaryRootSegments(
                untapered);
        GeneratedTreeBrush[] taperedRoots =
            GetPrimaryRootSegments(
                tapered);

        Assert.Equal(
            untaperedRoots.Length,
            taperedRoots.Length);

        for (
            int index = 0;
            index < untaperedRoots.Length;
            index++
        ) {
            Assert.Equal(
                untaperedRoots[index].BranchPath,
                taperedRoots[index].BranchPath);
            Assert.True(
                GetStartHalfExtent(
                    taperedRoots[index]) <
                GetStartHalfExtent(
                    untaperedRoots[index]));
        }
    }

    [Fact]
    public void GenerateMovesPrimaryBranchRootsWithTrunkBend()
    {
        TreeGenerationResult straight =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 76_543UL,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkTaper: 0.0,
                    trunkBend: 0.0,
                    trunkSegmentCount: 6));
        TreeGenerationResult bent =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 76_543UL,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkTaper: 0.0,
                    trunkBend: 0.20,
                    trunkSegmentCount: 6));
        GeneratedTreeBrush[] straightRoots =
            GetPrimaryRootSegments(straight);
        GeneratedTreeBrush[] bentRoots =
            GetPrimaryRootSegments(bent);
        bool moved = false;

        for (int index = 0; index < straightRoots.Length; index++) {
            Assert.Equal(
                straightRoots[index].BranchPath,
                bentRoots[index].BranchPath);

            if (
                !GetStartCenter(straightRoots[index])
                    .NearlyEquals(
                        GetStartCenter(bentRoots[index]),
                        CoordinateTolerance)
            ) {
                moved = true;
            }
        }

        Assert.True(moved);
    }

    [Fact]
    public void GenerateMovesPrimaryBranchRootsWithTrunkIrregularity()
    {
        TreeGenerationResult regular =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 91_827UL,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount: 6,
                    trunkIrregularity: 0.0,
                    trunkTwist: 1.0));
        TreeGenerationResult irregular =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 91_827UL,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount: 6,
                    trunkIrregularity: 0.25,
                    trunkTwist: 1.0));
        GeneratedTreeBrush[] regularRoots =
            GetPrimaryRootSegments(regular);
        GeneratedTreeBrush[] irregularRoots =
            GetPrimaryRootSegments(irregular);
        bool moved = false;

        for (int index = 0; index < regularRoots.Length; index++) {
            Assert.Equal(
                regularRoots[index].BranchPath,
                irregularRoots[index].BranchPath);

            if (
                !GetStartCenter(regularRoots[index])
                    .NearlyEquals(
                        GetStartCenter(irregularRoots[index]),
                        CoordinateTolerance)
            ) {
                moved = true;
            }
        }

        Assert.True(moved);
    }

    [Fact]
    public void GenerateEmbedsChildBranchRootsNearParentBranchSurface()
    {
        TreeGenerationSettings settings =
            CreateSettings(
                generationSeed: 44_221UL,
                trunkCrossSection:
                    TrunkCrossSectionProfile.Square,
                trunkSegmentCount: 6,
                trunkIrregularity: 0.15,
                trunkTwist: 0.50);
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                settings);
        TreeGenerationResult result =
            TreeGenerator.Generate(
                settings);

        foreach (
            PlannedTreeBranch child in
            skeleton.Branches.Where(
                branch =>
                    branch.Depth > 0)
        ) {
            GeneratedTreeBrush childRoot =
                result.Parts.Single(
                    part =>
                        part.Role == TreeBrushRole.Branch &&
                        string.Equals(
                            part.BranchPath,
                            child.Path,
                            StringComparison.Ordinal) &&
                        part.BranchSegmentIndex == 0);
            GeneratedTreeBrush[] parentSegments =
                result.Parts
                    .Where(
                        part =>
                            part.Role == TreeBrushRole.Branch &&
                            string.Equals(
                                part.BranchPath,
                                child.ParentPath,
                                StringComparison.Ordinal))
                    .OrderBy(
                        part =>
                            part.BranchSegmentIndex)
                    .ToArray();
            double scaledPosition =
                child.AttachmentFraction *
                parentSegments.Length;
            int segmentIndex =
                child.AttachmentFraction >= 1.0
                    ? parentSegments.Length - 1
                    : (int)Math.Floor(
                        scaledPosition);
            double localFraction =
                child.AttachmentFraction >= 1.0
                    ? 1.0
                    : scaledPosition - segmentIndex;
            GeneratedTreeBrush parentSegment =
                parentSegments[segmentIndex];
            Vector3d parentStart =
                GetStartCenter(
                    parentSegment);
            Vector3d parentEnd =
                GetEndCenter(
                    parentSegment);
            Vector3d parentDirection =
                (parentEnd - parentStart)
                    .Normalize();
            Vector3d parentCenter =
                parentStart +
                ((parentEnd - parentStart) * localFraction);
            double parentHalfExtent =
                Interpolate(
                    GetStartHalfExtent(parentSegment),
                    GetEndHalfExtent(parentSegment),
                    localFraction);
            Vector3d offset =
                GetStartCenter(childRoot) -
                parentCenter;
            double axialOffset =
                Math.Abs(
                    Vector3d.Dot(
                        offset,
                        parentDirection));
            Vector3d radialOffset =
                offset -
                (parentDirection * Vector3d.Dot(
                    offset,
                    parentDirection));
            double radialDistance =
                radialOffset.Length;
            double childHalfExtent =
                GetStartHalfExtent(
                    childRoot);
            int parentSideCount =
                parentSegment.Brush.FaceCount -
                2;
            double parentCircumradius =
                parentHalfExtent /
                Math.Cos(
                    Math.PI /
                    parentSideCount);

            Assert.True(
                axialOffset <=
                CoordinateTolerance);
            Assert.True(
                radialDistance <
                parentCircumradius +
                CoordinateTolerance);
            Assert.True(
                radialDistance >=
                Math.Max(
                    0.0,
                    parentHalfExtent -
                    childHalfExtent) -
                CoordinateTolerance);

            if (child.Depth >= 2) {
                Assert.True(
                    childHalfExtent <
                    parentHalfExtent);
            }
        }
    }

    private static GeneratedTreeBrush[] GetPrimaryRootSegments(
        TreeGenerationResult result)
    {
        return result.Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Branch &&
                    part.BranchPath is not null &&
                    !part.BranchPath.Contains('/') &&
                    part.BranchSegmentIndex == 0)
            .OrderBy(
                part =>
                    part.BranchPath!,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static Vector3d GetStartCenter(
        GeneratedTreeBrush branch)
    {
        PlanePoints3d cap =
            branch.Brush.Faces[0].PlanePoints;

        return (cap.Second + cap.Third) /
            2.0;
    }

    private static Vector3d GetEndCenter(
        GeneratedTreeBrush branch)
    {
        PlanePoints3d cap =
            branch.Brush.Faces[1].PlanePoints;

        return (cap.Second + cap.Third) /
            2.0;
    }

    private static double GetStartHalfExtent(
        GeneratedTreeBrush branch)
    {
        PlanePoints3d cap =
            branch.Brush.Faces[0].PlanePoints;

        return cap.First.DistanceTo(cap.Second) /
            2.0;
    }

    private static double GetEndHalfExtent(
        GeneratedTreeBrush branch)
    {
        PlanePoints3d cap =
            branch.Brush.Faces[1].PlanePoints;

        return cap.First.DistanceTo(cap.Second) /
            2.0;
    }

    private static double Interpolate(
        double start,
        double end,
        double fraction)
    {
        return start +
            ((end - start) * fraction);
    }

    private static TreeGenerationSettings CreateSettings(
        double trunkWidth = 32.0,
        ulong generationSeed = 1UL,
        TrunkCrossSectionProfile trunkCrossSection =
            TreeGenerationSettings.DefaultTrunkCrossSection,
        int trunkSegmentCount =
            TreeGenerationSettings.DefaultTrunkSegmentCount,
        double trunkTaper =
            TreeGenerationSettings.DefaultTrunkTaper,
        double trunkLean =
            TreeGenerationSettings.DefaultTrunkLean,
        double trunkBend =
            TreeGenerationSettings.DefaultTrunkBend,
        double trunkIrregularity =
            TreeGenerationSettings.DefaultTrunkIrregularity,
        double trunkTwist =
            TreeGenerationSettings.DefaultTrunkTwist)
    {
        return new TreeGenerationSettings(
            Vector3d.Zero,
            overallHeight: 256.0,
            trunkWidth: trunkWidth,
            canopyWidth: 192.0,
            canopyHeight: 128.0,
            canopyLayerCount: 3,
            generationSeed:
                new GenerationSeed(
                    generationSeed),
            gridSpacing: GridSpacing.Eight,
            trunkTextureName: "WOOD",
            canopyTextureName: "LEAF",
            trunkSegmentCount: trunkSegmentCount,
            trunkTaper: trunkTaper,
            trunkLean: trunkLean,
            trunkBend: trunkBend,
            trunkCrossSection: trunkCrossSection,
            trunkIrregularity: trunkIrregularity,
            trunkTwist: trunkTwist,
            detail: 1.0);
    }
}
