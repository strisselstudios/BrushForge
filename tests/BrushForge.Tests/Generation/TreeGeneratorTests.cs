using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Branches;
using BrushForge.MapFormat.Model;
using BrushForge.MapFormat.Serialization;
using BrushForge.MapFormat.Validation;

namespace BrushForge.Tests.Generation;

public sealed class TreeGeneratorTests
{
    [Fact]
    public void GenerateCreatesTrunkAndRequestedCanopyLayers()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    canopyLayerCount: 4));

        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();
        GeneratedTreeBrush[] branchParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Branch)
                .ToArray();
        GeneratedTreeBrush[] canopyParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();

        Assert.Equal(3, trunkParts.Length);
        Assert.Equal(35, branchParts.Length);
        Assert.Equal(4, canopyParts.Length);
        Assert.Equal(42, result.BrushCount);
        Assert.All(
            trunkParts.Concat(branchParts),
            part =>
                Assert.Equal(
                    -1,
                    part.CanopyLayerIndex));

        for (
            int layerIndex = 0;
            layerIndex < canopyParts.Length;
            layerIndex++
        ) {
            Assert.Equal(
                layerIndex,
                canopyParts[layerIndex].CanopyLayerIndex);
        }
    }

    [Fact]
    public void GenerateBuildsContiguousTaperedFacetedTrunkSegments()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings());
        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();

        Assert.Equal(3, trunkParts.Length);
        Assert.Equal(32.0, trunkParts[0].Bounds.Width);
        Assert.Equal(24.0, trunkParts[^1].Bounds.Width);
        Assert.All(
            trunkParts,
            part =>
            {
                Assert.Equal(10, part.Brush.FaceCount);
                Assert.True(
                    ConvexBrushValidator.Validate(
                        part.Brush)
                        .IsValid);
            });

        for (
            int segmentIndex = 1;
            segmentIndex < trunkParts.Length;
            segmentIndex++
        ) {
            Assert.Equal(
                trunkParts[segmentIndex - 1].Bounds.Maximum.Z,
                trunkParts[segmentIndex].Bounds.Minimum.Z);
        }
    }

    [Fact]
    public void GenerateProducesExportValidMapDocument()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings());

        MapExportValidationResult validation =
            MapExportValidator.Validate(
                result.Document);

        Assert.True(validation.IsValid);
        Assert.Single(result.Document.Entities);
        Assert.True(result.Document.Entities[0].IsWorldspawn);
    }

    [Fact]
    public void GenerateIsDeterministicForSameSettings()
    {
        TreeGenerationSettings settings =
            CreateSettings(
                generationSeed: 741UL,
                canopyLayerCount: 6);

        string first =
            Valve220MapWriter.Serialize(
                TreeGenerator.Generate(
                    settings)
                    .Document);

        string second =
            Valve220MapWriter.Serialize(
                TreeGenerator.Generate(
                    settings)
                    .Document);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GenerateUsesSeedToVaryCanopyGeometry()
    {
        TreeGenerationResult first =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 1UL,
                    canopyLayerCount: 8));

        TreeGenerationResult second =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 2UL,
                    canopyLayerCount: 8));

        Vector3d[] firstCenters =
            first.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .Select(
                    part =>
                        part.Bounds.Center)
                .ToArray();

        Vector3d[] secondCenters =
            second.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .Select(
                    part =>
                        part.Bounds.Center)
                .ToArray();

        Assert.False(
            firstCenters.SequenceEqual(
                secondCenters));
    }

    [Fact]
    public void GenerateSnapsOriginAndVerticesToSupportedGridPrecision()
    {
        TreeGenerationSettings settings =
            CreateSettings()
                .WithOrigin(
                    new Vector3d(
                        5.0,
                        11.0,
                        19.0));

        TreeGenerationResult result =
            TreeGenerator.Generate(
                settings);

        Assert.Equal(
            8.0,
            result.Parts[0].Bounds.Center.X);
        Assert.Equal(
            8.0,
            result.Parts[0].Bounds.Center.Y);
        Assert.Equal(
            16.0,
            result.Parts[0].Bounds.Minimum.Z);

        GridSpacing trunkHorizontalSpacing =
            new(
                settings.GridSpacing.Units /
                (
                    settings.TrunkCrossSection ==
                        TrunkCrossSectionProfile.Octagonal
                        ? 4.0
                        : 2.0
                ));

        foreach (GeneratedTreeBrush part in result.Parts) {
            BrushValidationResult validation =
                ConvexBrushValidator.Validate(
                    part.Brush);

            Assert.True(validation.IsValid);

            if (part.Role == TreeBrushRole.Branch) {
                continue;
            }

            GridSpacing horizontalSpacing =
                part.Role == TreeBrushRole.Trunk
                    ? trunkHorizontalSpacing
                    : settings.GridSpacing;

            Assert.All(
                validation.Geometry!.Vertices,
                vertex =>
                {
                    Assert.True(
                        horizontalSpacing.IsAligned(
                            vertex.X));
                    Assert.True(
                        horizontalSpacing.IsAligned(
                            vertex.Y));
                    Assert.True(
                        settings.GridSpacing.IsAligned(
                            vertex.Z));
                });
        }
    }

    [Fact]
    public void GenerateKeepsEveryTrunkSegmentCenteredOnSnappedOrigin()
    {
        TreeGenerationSettings settings =
            CreateSettings()
                .WithOrigin(
                    new Vector3d(
                        5.0,
                        11.0,
                        19.0));

        TreeGenerationResult result =
            TreeGenerator.Generate(
                settings);

        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();

        Assert.True(
            trunkParts.Length >= 2);

        Assert.All(
            trunkParts,
            part =>
            {
                Assert.Equal(
                    8.0,
                    part.Bounds.Center.X,
                    precision: 6);
                Assert.Equal(
                    8.0,
                    part.Bounds.Center.Y,
                    precision: 6);
            });
    }

    [Fact]
    public void GenerateTapersOppositeTrunkSidesByEqualDistances()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings());

        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();

        Assert.True(
            trunkParts.Length >= 2);

        var baseBounds =
            trunkParts[0].Bounds;

        var topBounds =
            trunkParts[^1].Bounds;

        double negativeXReduction =
            topBounds.Minimum.X -
            baseBounds.Minimum.X;

        double positiveXReduction =
            baseBounds.Maximum.X -
            topBounds.Maximum.X;

        double negativeYReduction =
            topBounds.Minimum.Y -
            baseBounds.Minimum.Y;

        double positiveYReduction =
            baseBounds.Maximum.Y -
            topBounds.Maximum.Y;

        Assert.True(
            negativeXReduction > 0.0);
        Assert.True(
            negativeYReduction > 0.0);

        Assert.Equal(
            negativeXReduction,
            positiveXReduction,
            precision: 6);

        Assert.Equal(
            negativeYReduction,
            positiveYReduction,
            precision: 6);

        Assert.Equal(
            baseBounds.Center.X,
            topBounds.Center.X,
            precision: 6);

        Assert.Equal(
            baseBounds.Center.Y,
            topBounds.Center.Y,
            precision: 6);
    }
    [Fact]
    public void GeneratePreservesRequestedOverallHeightAfterSnapping()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    overallHeight: 259.0));

        Assert.Equal(256.0, result.Bounds.Height);
    }

    [Fact]
    public void GenerateAssignsTexturesBySemanticRole()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkTextureName: "BARK",
                    canopyTextureName: "{LEAVES"));

        foreach (GeneratedTreeBrush part in result.Parts) {
            string expectedTexture =
                part.Role == TreeBrushRole.Canopy
                    ? "{LEAVES"
                    : "BARK";

            Assert.All(
                part.Brush.Faces,
                face =>
                    Assert.Equal(
                        expectedTexture,
                        face.TextureName));
        }
    }

    [Theory]
    [InlineData(0.0, 6)]
    [InlineData(0.49, 6)]
    [InlineData(0.50, 10)]
    [InlineData(1.0, 10)]
    public void GenerateUsesDetailToResolveOctagonalTrunkComplexity(
        double detail,
        int expectedFaceCount)
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Octagonal,
                    detail: detail));

        GeneratedTreeBrush[] trunkParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .ToArray();

        Assert.All(
            trunkParts,
            part =>
                Assert.Equal(
                    expectedFaceCount,
                    part.Brush.FaceCount));
    }

    [Fact]
    public void GenerateDoesNotIncreaseExplicitSquareTrunkComplexity()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    detail: 1.0));

        Assert.All(
            result.Parts.Where(
                part =>
                    part.Role == TreeBrushRole.Trunk),
            part =>
                Assert.Equal(
                    6,
                    part.Brush.FaceCount));
    }

    [Fact]
    public void ChangingDetailPreservesCanopyEnvelope()
    {
        TreeGenerationResult minimum =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Octagonal,
                    detail: 0.0));

        TreeGenerationResult maximum =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Octagonal,
                    detail: 1.0));

        Assert.Equal(
            GetCanopyEnvelope(maximum),
            GetCanopyEnvelope(minimum));
    }

    [Theory]
    [InlineData(0.0, 2)]
    [InlineData(0.25, 3)]
    [InlineData(0.50, 4)]
    [InlineData(0.75, 5)]
    [InlineData(1.0, 6)]
    public void GenerateScalesRequestedTrunkSegmentsWithDetail(
        double detail,
        int expectedSegmentCount)
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount: 6,
                    trunkIrregularity: 0.0,
                    detail: detail));

        int actualSegmentCount =
            result.Parts.Count(
                part =>
                    part.Role == TreeBrushRole.Trunk);

        Assert.Equal(
            expectedSegmentCount,
            actualSegmentCount);
    }

    [Fact]
    public void DetailDoesNotIncreaseMinimumRequestedTrunkSegments()
    {
        TreeGenerationResult minimumDetail =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount:
                        TreeGenerationSettings.MinimumTrunkSegmentCount,
                    detail: 0.0));
        TreeGenerationResult maximumDetail =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount:
                        TreeGenerationSettings.MinimumTrunkSegmentCount,
                    detail: 1.0));

        Assert.Equal(
            TreeGenerationSettings.MinimumTrunkSegmentCount,
            minimumDetail.Parts.Count(
                part =>
                    part.Role == TreeBrushRole.Trunk));
        Assert.Equal(
            TreeGenerationSettings.MinimumTrunkSegmentCount,
            maximumDetail.Parts.Count(
                part =>
                    part.Role == TreeBrushRole.Trunk));
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.25, 0.0625)]
    [InlineData(0.50, 0.125)]
    [InlineData(0.75, 0.1875)]
    [InlineData(1.0, 0.25)]
    public void GenerateScalesTrunkIrregularityContinuouslyWithDetail(
        double detail,
        double expectedIrregularity)
    {
        TreeGenerationSettings actualSettings =
            CreateSettings(
                generationSeed: 741UL,
                canopyLayerCount: 1,
                trunkCrossSection:
                    TrunkCrossSectionProfile.Square,
                trunkSegmentCount:
                    TreeGenerationSettings.MinimumTrunkSegmentCount,
                trunkIrregularity: 0.25,
                trunkTwist: 0.0,
                detail: detail);
        TreeGenerationSettings expectedSettings =
            CreateSettings(
                generationSeed: 741UL,
                canopyLayerCount: 1,
                trunkCrossSection:
                    TrunkCrossSectionProfile.Square,
                trunkSegmentCount:
                    TreeGenerationSettings.MinimumTrunkSegmentCount,
                trunkIrregularity: expectedIrregularity,
                trunkTwist: 0.0,
                detail: 1.0);

        string actual =
            CreateRoleMapSignature(
                TreeGenerator.Generate(
                    actualSettings),
                TreeBrushRole.Trunk);
        string expected =
            CreateRoleMapSignature(
                TreeGenerator.Generate(
                    expectedSettings),
                TreeBrushRole.Trunk);

        Assert.Equal(
            expected,
            actual);
    }

    [Fact]
    public void ChangingIrregularityDetailPreservesCanopyPlacement()
    {
        TreeGenerationResult minimum =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    canopyLayerCount: 1,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount: 6,
                    trunkIrregularity: 0.25,
                    trunkTwist: 1.0,
                    detail: 0.0));
        TreeGenerationResult maximum =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    canopyLayerCount: 1,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount: 6,
                    trunkIrregularity: 0.25,
                    trunkTwist: 1.0,
                    detail: 1.0));

        GeneratedTreeBrush[] minimumCanopy =
            minimum.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();
        GeneratedTreeBrush[] maximumCanopy =
            maximum.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();

        Assert.Equal(
            maximumCanopy.Length,
            minimumCanopy.Length);

        for (int index = 0; index < maximumCanopy.Length; index++) {
            Assert.Equal(
                maximumCanopy[index].Bounds,
                minimumCanopy[index].Bounds);
        }
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.25, 0.25)]
    [InlineData(0.50, 0.50)]
    [InlineData(0.75, 0.75)]
    [InlineData(1.0, 1.0)]
    public void GenerateScalesTrunkTwistContinuouslyWithDetail(
        double detail,
        double expectedTwist)
    {
        double expectedIrregularity =
            0.25 *
            detail;

        TreeGenerationSettings actualSettings =
            CreateSettings(
                generationSeed: 741UL,
                canopyLayerCount: 1,
                trunkCrossSection:
                    TrunkCrossSectionProfile.Square,
                trunkSegmentCount:
                    TreeGenerationSettings.MinimumTrunkSegmentCount,
                trunkIrregularity: 0.25,
                trunkTwist: 1.0,
                detail: detail);
        TreeGenerationSettings expectedSettings =
            CreateSettings(
                generationSeed: 741UL,
                canopyLayerCount: 1,
                trunkCrossSection:
                    TrunkCrossSectionProfile.Square,
                trunkSegmentCount:
                    TreeGenerationSettings.MinimumTrunkSegmentCount,
                trunkIrregularity: expectedIrregularity,
                trunkTwist: expectedTwist,
                detail: 1.0);

        string actual =
            CreateRoleMapSignature(
                TreeGenerator.Generate(
                    actualSettings),
                TreeBrushRole.Trunk);
        string expected =
            CreateRoleMapSignature(
                TreeGenerator.Generate(
                    expectedSettings),
                TreeBrushRole.Trunk);

        Assert.Equal(
            expected,
            actual);
    }

    [Fact]
    public void ChangingTwistDetailPreservesCanopyPlacement()
    {
        TreeGenerationResult minimum =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    canopyLayerCount: 1,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount:
                        TreeGenerationSettings.MinimumTrunkSegmentCount,
                    trunkIrregularity: 0.25,
                    trunkTwist: 1.0,
                    detail: 0.0));
        TreeGenerationResult maximum =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    canopyLayerCount: 1,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount:
                        TreeGenerationSettings.MinimumTrunkSegmentCount,
                    trunkIrregularity: 0.25,
                    trunkTwist: 1.0,
                    detail: 1.0));

        GeneratedTreeBrush[] minimumCanopy =
            minimum.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();
        GeneratedTreeBrush[] maximumCanopy =
            maximum.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();

        Assert.Equal(
            maximumCanopy.Length,
            minimumCanopy.Length);

        for (int index = 0; index < maximumCanopy.Length; index++) {
            Assert.Equal(
                maximumCanopy[index].Bounds,
                minimumCanopy[index].Bounds);
        }
    }

    [Theory]
    [InlineData(0.0, 1)]
    [InlineData(0.25, 3)]
    [InlineData(0.50, 5)]
    [InlineData(0.75, 6)]
    [InlineData(1.0, 8)]
    public void GenerateScalesCanopyLayerComplexityWithDetail(
        double detail,
        int expectedLayerCount)
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    canopyLayerCount: 8,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount:
                        TreeGenerationSettings.MinimumTrunkSegmentCount,
                    detail: detail));

        GeneratedTreeBrush[] canopyParts =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();

        Assert.Equal(
            expectedLayerCount,
            canopyParts.Length);

        for (int index = 0; index < canopyParts.Length; index++) {
            Assert.Equal(
                index,
                canopyParts[index].CanopyLayerIndex);
        }
    }

    [Fact]
    public void CanopyDetailDoesNotIncreaseSingleRequestedLayer()
    {
        TreeGenerationResult minimum =
            TreeGenerator.Generate(
                CreateSettings(
                    canopyLayerCount: 1,
                    detail: 0.0));
        TreeGenerationResult maximum =
            TreeGenerator.Generate(
                CreateSettings(
                    canopyLayerCount: 1,
                    detail: 1.0));

        Assert.Single(
            minimum.Parts,
            part =>
                part.Role == TreeBrushRole.Canopy);
        Assert.Single(
            maximum.Parts,
            part =>
                part.Role == TreeBrushRole.Canopy);
    }

    [Fact]
    public void ReducedCanopyDetailMergesFullDetailLayersWithoutRerolling()
    {
        TreeGenerationResult reduced =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    canopyLayerCount: 8,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount:
                        TreeGenerationSettings.MinimumTrunkSegmentCount,
                    detail: 0.40));
        TreeGenerationResult full =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 741UL,
                    canopyLayerCount: 8,
                    trunkCrossSection:
                        TrunkCrossSectionProfile.Square,
                    trunkSegmentCount:
                        TreeGenerationSettings.MinimumTrunkSegmentCount,
                    detail: 1.0));

        GeneratedTreeBrush[] reducedCanopy =
            reduced.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();
        GeneratedTreeBrush[] fullCanopy =
            full.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();

        Assert.Equal(4, reducedCanopy.Length);
        Assert.Equal(8, fullCanopy.Length);

        for (int index = 0; index < reducedCanopy.Length; index++) {
            Bounds3d expectedBounds =
                fullCanopy[index * 2]
                    .Bounds
                    .Union(
                        fullCanopy[(index * 2) + 1]
                            .Bounds);

            Assert.Equal(
                expectedBounds,
                reducedCanopy[index].Bounds);
        }
    }

    [Fact]
    public void GenerateRealizesFiveSegmentedValidPrimaryBranchPathsAtFullDetail()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 1234UL,
                    detail: 1.0));

        GeneratedTreeBrush[] branches =
            GetPrimaryBranchParts(result);
        IGrouping<string, GeneratedTreeBrush>[] paths =
            branches
                .GroupBy(
                    branch =>
                        branch.BranchPath!,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(
            15,
            branches.Length);
        Assert.Equal(
            5,
            paths.Length);
        Assert.All(
            paths,
            path =>
            {
                GeneratedTreeBrush[] segments =
                    path
                        .OrderBy(
                            branch =>
                                branch.BranchSegmentIndex)
                        .ToArray();

                Assert.Equal(3, segments.Length);
                Assert.Equal(
                    0,
                    segments[0].BranchSegmentIndex);
                Assert.Equal(
                    1,
                    segments[1].BranchSegmentIndex);
                Assert.Equal(
                    2,
                    segments[2].BranchSegmentIndex);
                Assert.All(
                    segments,
                    branch =>
                    {
                        Assert.Equal(
                            -1,
                            branch.CanopyLayerIndex);
                        Assert.Equal(
                            3,
                            branch.BranchSegmentCount);
                        Assert.Equal(
                            6,
                            branch.Brush.FaceCount);
                        Assert.True(
                            ConvexBrushValidator.Validate(
                                branch.Brush)
                                .IsValid);
                    });
            });
    }

    [Fact]
    public void GenerateUsesSeedToVaryPrimaryBranchGeometry()
    {
        TreeGenerationResult first =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 101UL));
        TreeGenerationResult second =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 202UL));

        Bounds3d[] firstBounds =
            GetBranchBounds(first);
        Bounds3d[] secondBounds =
            GetBranchBounds(second);

        Assert.False(
            firstBounds.SequenceEqual(
                secondBounds));
    }

    [Fact]
    public void GenerateKeepsPrimaryBranchIdentitiesStableAcrossDetailChanges()
    {
        TreeGenerationSettings minimumSettings =
            CreateSettings(
                generationSeed: 741UL,
                trunkCrossSection:
                    TrunkCrossSectionProfile.Square,
                trunkSegmentCount:
                    TreeGenerationSettings.MinimumTrunkSegmentCount,
                trunkIrregularity: 0.0,
                trunkTwist: 0.0,
                detail: 0.0);
        TreeGenerationSettings maximumSettings =
            minimumSettings.WithDetail(1.0);

        string[] minimumPaths =
            GetPrimaryBranchParts(
                TreeGenerator.Generate(
                    minimumSettings))
                .Select(
                    branch =>
                        branch.BranchPath!)
                .Distinct(
                    StringComparer.Ordinal)
                .ToArray();
        string[] maximumPaths =
            GetPrimaryBranchParts(
                TreeGenerator.Generate(
                    maximumSettings))
                .Select(
                    branch =>
                        branch.BranchPath!)
                .Distinct(
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(
            minimumPaths,
            maximumPaths);
    }

    [Fact]
    public void GenerateUsesTrunkTextureForPrimaryBranches()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkTextureName: "BRANCH_BARK",
                    canopyTextureName: "LEAF"));

        GeneratedTreeBrush[] branches =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Branch)
                .ToArray();

        Assert.All(
            branches,
            branch =>
                Assert.All(
                    branch.Brush.Faces,
                    face =>
                        Assert.Equal(
                            "BRANCH_BARK",
                            face.TextureName)));
    }

    [Fact]
    public void GeneratePlacesPrimaryBranchesAroundMultipleSidesOfTrunk()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 12_345UL));
        Bounds3d[] branches =
            GetPrimaryBranchBounds(result);
        Vector3d trunkCenter =
            result.Parts
                .First(
                    part =>
                        part.Role == TreeBrushRole.Trunk)
                .Bounds
                .Center;

        Assert.Contains(
            branches,
            bounds =>
                bounds.Center.X < trunkCenter.X);
        Assert.Contains(
            branches,
            bounds =>
                bounds.Center.X > trunkCenter.X);
        Assert.Contains(
            branches,
            bounds =>
                bounds.Center.Y < trunkCenter.Y);
        Assert.Contains(
            branches,
            bounds =>
                bounds.Center.Y > trunkCenter.Y);
    }

    [Fact]
    public void GenerateRealizesTenSegmentedValidSecondaryBranchPathsAtFullDetail()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 8_080UL,
                    detail: 1.0));

        GeneratedTreeBrush[] secondaries =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Branch &&
                        part.BranchPath is not null &&
                        part.BranchPath.Contains('/'))
                .ToArray();
        IGrouping<string, GeneratedTreeBrush>[] paths =
            secondaries
                .GroupBy(
                    branch =>
                        branch.BranchPath!,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(
            20,
            secondaries.Length);
        Assert.Equal(
            10,
            paths.Length);
        Assert.All(
            paths,
            path =>
            {
                GeneratedTreeBrush[] segments =
                    path
                        .OrderBy(
                            branch =>
                                branch.BranchSegmentIndex)
                        .ToArray();

                Assert.Equal(2, segments.Length);
                Assert.Equal(
                    0,
                    segments[0].BranchSegmentIndex);
                Assert.Equal(
                    1,
                    segments[1].BranchSegmentIndex);
                Assert.All(
                    segments,
                    branch =>
                    {
                        Assert.True(
                            branch.BranchPath!.Contains(
                                "/S",
                                StringComparison.Ordinal));
                        Assert.Equal(
                            2,
                            branch.BranchSegmentCount);
                        Assert.Equal(
                            6,
                            branch.Brush.FaceCount);
                        Assert.True(
                            ConvexBrushValidator.Validate(
                                branch.Brush)
                                .IsValid);
                    });
            });
    }

    [Fact]
    public void GenerateKeepsSecondaryBranchesHiddenAtMinimumDetail()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 8_080UL,
                    detail: 0.0));

        GeneratedTreeBrush[] branches =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Branch)
                .ToArray();

        Assert.Equal(
            5,
            branches.Length);
        Assert.DoesNotContain(
            branches,
            branch =>
                branch.BranchPath is not null &&
                branch.BranchPath.Contains('/'));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.35)]
    [InlineData(0.55)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void GenerateRealizesExactlyBranchesAllowedByStableDetailThreshold(
        double detail)
    {
        TreeGenerationSettings settings =
            CreateSettings(
                generationSeed: 44_444UL,
                detail: detail);
        TreeBranchSkeleton skeleton =
            TreeBranchSkeletonPlanner.Create(
                settings);
        string[] expectedPaths =
            skeleton.Branches
                .Where(
                    branch =>
                        branch.RequiredDetail <= detail)
                .Select(
                    branch =>
                        branch.Path)
                .ToArray();

        string[] actualPaths =
            TreeGenerator.Generate(
                settings)
                .Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Branch)
                .Select(
                    part =>
                        part.BranchPath!)
                .Distinct(
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(
            expectedPaths,
            actualPaths);
    }

    [Theory]
    [InlineData(0.0, 1)]
    [InlineData(0.35, 2)]
    [InlineData(0.55, 2)]
    [InlineData(0.75, 3)]
    [InlineData(1.0, 3)]
    public void GenerateScalesPrimaryBranchSegmentationWithDetail(
        double detail,
        int expectedSegmentsPerPath)
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 51_515UL,
                    detail: detail));
        IGrouping<string, GeneratedTreeBrush>[] paths =
            GetPrimaryBranchParts(result)
                .GroupBy(
                    branch =>
                        branch.BranchPath!,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(5, paths.Length);
        Assert.All(
            paths,
            path =>
            {
                GeneratedTreeBrush[] segments =
                    path.ToArray();

                Assert.Equal(
                    expectedSegmentsPerPath,
                    segments.Length);
                Assert.All(
                    segments,
                    segment =>
                        Assert.Equal(
                            expectedSegmentsPerPath,
                            segment.BranchSegmentCount));
            });
    }

    [Theory]
    [InlineData(0.32, 0.35)]
    [InlineData(0.65, 0.70)]
    public void GenerateKeepsSecondaryGeometryStableWhenParentSegmentationRefines(
        double lowerDetail,
        double higherDetail)
    {
        TreeGenerationSettings lowerSettings =
            CreateSettings(
                generationSeed: 44_444UL,
                trunkCrossSection:
                    TrunkCrossSectionProfile.Square,
                trunkSegmentCount:
                    TreeGenerationSettings.MinimumTrunkSegmentCount,
                trunkIrregularity: 0.0,
                trunkTwist: 0.0,
                detail: lowerDetail);
        TreeGenerationSettings higherSettings =
            lowerSettings.WithDetail(
                higherDetail);

        Dictionary<string, GeneratedTreeBrush[]> lowerPaths =
            GetSecondaryBranchParts(
                TreeGenerator.Generate(
                    lowerSettings))
                .GroupBy(
                    branch =>
                        branch.BranchPath!,
                    StringComparer.Ordinal)
                .ToDictionary(
                    path =>
                        path.Key,
                    path =>
                        path
                            .OrderBy(
                                segment =>
                                    segment.BranchSegmentIndex)
                            .ToArray(),
                    StringComparer.Ordinal);
        Dictionary<string, GeneratedTreeBrush[]> higherPaths =
            GetSecondaryBranchParts(
                TreeGenerator.Generate(
                    higherSettings))
                .GroupBy(
                    branch =>
                        branch.BranchPath!,
                    StringComparer.Ordinal)
                .ToDictionary(
                    path =>
                        path.Key,
                    path =>
                        path
                            .OrderBy(
                                segment =>
                                    segment.BranchSegmentIndex)
                            .ToArray(),
                    StringComparer.Ordinal);

        string[] comparablePaths =
            lowerPaths.Keys
                .Where(
                    path =>
                        higherPaths.ContainsKey(path) &&
                        lowerPaths[path].Length ==
                        higherPaths[path].Length)
                .ToArray();

        Assert.NotEmpty(comparablePaths);
        Assert.All(
            comparablePaths,
            path =>
            {
                GeneratedTreeBrush[] lowerSegments =
                    lowerPaths[path];
                GeneratedTreeBrush[] higherSegments =
                    higherPaths[path];

                Assert.Equal(
                    lowerSegments.Length,
                    higherSegments.Length);

                for (
                    int segmentIndex = 0;
                    segmentIndex < lowerSegments.Length;
                    segmentIndex++
                ) {
                    Assert.Equal(
                        lowerSegments[segmentIndex].BranchSegmentIndex,
                        higherSegments[segmentIndex].BranchSegmentIndex);
                    Assert.Equal(
                        lowerSegments[segmentIndex].Bounds,
                        higherSegments[segmentIndex].Bounds);
                }
            });
    }

    [Fact]
    public void GenerateBendsPrimaryBranchPathsAtFullDetail()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 7_777UL,
                    detail: 1.0));
        IGrouping<string, GeneratedTreeBrush>[] paths =
            GetPrimaryBranchParts(result)
                .GroupBy(
                    branch =>
                        branch.BranchPath!,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.All(
            paths,
            path =>
            {
                Vector3d[] centers =
                    path
                        .OrderBy(
                            segment =>
                                segment.BranchSegmentIndex)
                        .Select(
                            segment =>
                                segment.Bounds.Center)
                        .ToArray();
                Vector3d firstDirection =
                    centers[1] - centers[0];
                Vector3d secondDirection =
                    centers[2] - centers[1];

                Assert.True(
                    Vector3d.Cross(
                        firstDirection,
                        secondDirection)
                        .Length > 0.001);
            });
    }

    [Fact]
    public void GenerateStoresSequentialBranchSegmentMetadata()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 90_909UL,
                    detail: 1.0));
        IGrouping<string, GeneratedTreeBrush>[] paths =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Branch)
                .GroupBy(
                    part =>
                        part.BranchPath!,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.All(
            paths,
            path =>
            {
                GeneratedTreeBrush[] segments =
                    path
                        .OrderBy(
                            segment =>
                                segment.BranchSegmentIndex)
                        .ToArray();
                int expectedCount =
                    segments.Length;

                Assert.Equal(
                    Enumerable.Range(
                        0,
                        expectedCount),
                    segments.Select(
                        segment =>
                            segment.BranchSegmentIndex));
                Assert.All(
                    segments,
                    segment =>
                        Assert.Equal(
                            expectedCount,
                            segment.BranchSegmentCount));
            });
    }

    [Fact]
    public void GenerateReproducesSegmentedBranchGeometryForSameSeed()
    {
        TreeGenerationSettings settings =
            CreateSettings(
                generationSeed: 61_234UL,
                detail: 1.0);
        GeneratedTreeBrush[] first =
            TreeGenerator.Generate(
                settings)
                .Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Branch)
                .ToArray();
        GeneratedTreeBrush[] second =
            TreeGenerator.Generate(
                settings)
                .Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Branch)
                .ToArray();

        Assert.Equal(
            first.Length,
            second.Length);

        for (int index = 0; index < first.Length; index++) {
            Assert.Equal(
                first[index].BranchPath,
                second[index].BranchPath);
            Assert.Equal(
                first[index].BranchSegmentIndex,
                second[index].BranchSegmentIndex);
            Assert.Equal(
                first[index].BranchSegmentCount,
                second[index].BranchSegmentCount);
            Assert.Equal(
                first[index].Bounds,
                second[index].Bounds);
        }
    }

    [Fact]
    public void GenerateMakesSecondaryBranchesGrowOutwardFromTrunk()
    {
        for (ulong generationSeed = 1UL; generationSeed <= 32UL; generationSeed++) {
            TreeGenerationResult result =
                TreeGenerator.Generate(
                    CreateSettings(
                        generationSeed: generationSeed,
                        trunkCrossSection:
                            TrunkCrossSectionProfile.Square,
                        trunkSegmentCount:
                            TreeGenerationSettings.MinimumTrunkSegmentCount,
                        trunkIrregularity: 0.0,
                        trunkTwist: 0.0,
                        detail: 1.0));

            IGrouping<string, GeneratedTreeBrush>[] paths =
                GetSecondaryBranchParts(result)
                    .GroupBy(
                        branch =>
                            branch.BranchPath!,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                TreeBranchSkeletonPlanner.PrimaryBranchCount *
                TreeBranchSkeletonPlanner.SecondaryBranchesPerPrimary,
                paths.Length);

            Assert.All(
                paths,
                path =>
                {
                    GeneratedTreeBrush[] segments =
                        path
                            .OrderBy(
                                segment =>
                                    segment.BranchSegmentIndex)
                            .ToArray();
                    Vector3d firstCenter =
                        segments[0].Bounds.Center;
                    Vector3d lastCenter =
                        segments[^1].Bounds.Center;
                    double firstRadiusSquared =
                        (firstCenter.X * firstCenter.X) +
                        (firstCenter.Y * firstCenter.Y);
                    double lastRadiusSquared =
                        (lastCenter.X * lastCenter.X) +
                        (lastCenter.Y * lastCenter.Y);

                    Assert.True(
                        lastRadiusSquared >
                        firstRadiusSquared);
                });
        }
    }

    [Fact]
    public void GeneratePreventsNetDownwardSecondaryGrowth()
    {
        for (ulong generationSeed = 1UL; generationSeed <= 32UL; generationSeed++) {
            TreeGenerationResult result =
                TreeGenerator.Generate(
                    CreateSettings(
                        generationSeed: generationSeed,
                        trunkCrossSection:
                            TrunkCrossSectionProfile.Square,
                        trunkSegmentCount:
                            TreeGenerationSettings.MinimumTrunkSegmentCount,
                        trunkIrregularity: 0.0,
                        trunkTwist: 0.0,
                        detail: 1.0));

            IGrouping<string, GeneratedTreeBrush>[] paths =
                GetSecondaryBranchParts(result)
                    .GroupBy(
                        branch =>
                            branch.BranchPath!,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.All(
                paths,
                path =>
                {
                    GeneratedTreeBrush[] segments =
                        path
                            .OrderBy(
                                segment =>
                                    segment.BranchSegmentIndex)
                            .ToArray();
                    Vector3d firstCenter =
                        segments[0].Bounds.Center;
                    Vector3d lastCenter =
                        segments[^1].Bounds.Center;

                    Assert.True(
                        lastCenter.Z >=
                        firstCenter.Z);
                });
        }
    }

    [Fact]
    public void GenerateStoresGeneratorMetadataInWorldspawn()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    generationSeed: 123456UL));

        MapEntity worldspawn =
            Assert.IsType<MapEntity>(
                result.Document.Worldspawn);

        Assert.True(
            worldspawn.TryGetProperty(
                "_brushforge_generator",
                out string? generator));

        Assert.True(
            worldspawn.TryGetProperty(
                "_brushforge_seed",
                out string? seed));

        Assert.Equal("tree", generator);
        Assert.Equal("123456", seed);
    }

    private static GeneratedTreeBrush[] GetPrimaryBranchParts(
        TreeGenerationResult result)
    {
        return result.Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Branch &&
                    part.BranchPath is not null &&
                    !part.BranchPath.Contains('/'))
            .ToArray();
    }

    private static GeneratedTreeBrush[] GetSecondaryBranchParts(
        TreeGenerationResult result)
    {
        return result.Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Branch &&
                    part.BranchPath is not null &&
                    part.BranchPath.Contains('/'))
            .ToArray();
    }

    private static Bounds3d[] GetPrimaryBranchBounds(
        TreeGenerationResult result)
    {
        return GetPrimaryBranchParts(result)
            .Select(
                part =>
                    part.Bounds)
            .ToArray();
    }

    private static string CreateRoleMapSignature(
        TreeGenerationResult result,
        TreeBrushRole role)
    {
        MapEntity worldspawn =
            MapEntity.CreateWorldspawn(
                result.Parts
                    .Where(
                        part =>
                            part.Role == role)
                    .Select(
                        part =>
                            part.Brush));
        MapDocument document = new(
        [
            worldspawn
        ]);

        return Valve220MapWriter.Serialize(
            document);
    }

    private static Bounds3d[] GetBranchBounds(
        TreeGenerationResult result)
    {
        return result.Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Branch)
            .Select(
                part =>
                    part.Bounds)
            .ToArray();
    }

    private static Bounds3d GetCanopyEnvelope(
        TreeGenerationResult result)
    {
        GeneratedTreeBrush[] canopy =
            result.Parts
                .Where(
                    part =>
                        part.Role == TreeBrushRole.Canopy)
                .ToArray();

        Bounds3d envelope =
            canopy[0].Bounds;

        for (int index = 1; index < canopy.Length; index++) {
            envelope =
                envelope.Union(
                    canopy[index].Bounds);
        }

        return envelope;
    }

    private static TreeGenerationSettings CreateSettings(
        double overallHeight = 256.0,
        double trunkWidth = 32.0,
        double canopyWidth = 160.0,
        double canopyHeight = 128.0,
        int canopyLayerCount = 3,
        ulong generationSeed = 1UL,
        string trunkTextureName = "WOOD",
        string canopyTextureName = "LEAF",
        GridSpacing? gridSpacing = null,
        TrunkCrossSectionProfile trunkCrossSection =
            TreeGenerationSettings.DefaultTrunkCrossSection,
        int trunkSegmentCount =
            TreeGenerationSettings.DefaultTrunkSegmentCount,
        double trunkIrregularity =
            TreeGenerationSettings.DefaultTrunkIrregularity,
        double trunkTwist =
            TreeGenerationSettings.DefaultTrunkTwist,
        double detail = TreeGenerationSettings.DefaultDetail)
    {
        return new TreeGenerationSettings(
            Vector3d.Zero,
            overallHeight,
            trunkWidth,
            canopyWidth,
            canopyHeight,
            canopyLayerCount,
            new GenerationSeed(
                generationSeed),
            gridSpacing ?? GridSpacing.Eight,
            trunkTextureName,
            canopyTextureName,
            trunkSegmentCount: trunkSegmentCount,
            trunkCrossSection: trunkCrossSection,
            trunkIrregularity: trunkIrregularity,
            trunkTwist: trunkTwist,
            detail: detail);
    }
}
