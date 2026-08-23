using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Input;

namespace BrushForge.Tests.Generation;

public sealed class TrunkIrregularityControlTests
{
    private const double CoordinateTolerance = 1e-8;

    [Fact]
    public void SettingsUseDocumentedTrunkIrregularityDefaults()
    {
        TreeGenerationSettings settings =
            CreateSettings();

        Assert.Equal(
            TreeGenerationSettings.DefaultTrunkIrregularity,
            settings.TrunkIrregularity);
        Assert.Equal(
            TreeGenerationSettings.DefaultTrunkTwist,
            settings.TrunkTwist);
        Assert.Equal(
            0.0,
            TreeGenerationSettings.MinimumTrunkIrregularity);
        Assert.Equal(
            0.25,
            TreeGenerationSettings.MaximumTrunkIrregularity);
        Assert.Equal(
            0.0,
            TreeGenerationSettings.MinimumTrunkTwist);
        Assert.Equal(
            1.0,
            TreeGenerationSettings.MaximumTrunkTwist);
    }

    [Fact]
    public void SettingsRejectUnsupportedTrunkIrregularityValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkIrregularity: -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkIrregularity: 0.26));
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkIrregularity: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkTwist: -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkTwist: 1.01));
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkTwist:
                        double.PositiveInfinity));
    }

    [Fact]
    public void ParserReadsExplicitTrunkIrregularityControls()
    {
        TreeGenerationSettings settings =
            TreeGenerationInputParser.Parse(
                new TreeGenerationInput(
                    "42",
                    "256",
                    "32",
                    "160",
                    "128",
                    "3",
                    "8",
                    "WOOD",
                    "LEAF",
                    TrunkIrregularityPercent: "25",
                    TrunkTwistPercent: "100"));

        Assert.Equal(
            0.25,
            settings.TrunkIrregularity);
        Assert.Equal(
            1.0,
            settings.TrunkTwist);
    }

    [Fact]
    public void ZeroIrregularityPreservesGeometryRegardlessOfTwist()
    {
        TreeGenerationResult untwisted =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkIrregularity: 0.0,
                    trunkTwist: 0.0));
        TreeGenerationResult fullyTwisted =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkIrregularity: 0.0,
                    trunkTwist: 1.0));

        AssertGenerationResultsEqual(
            untwisted,
            fullyTwisted);
    }

    [Fact]
    public void SameSeedReproducesIrregularTrunkGeometry()
    {
        TreeGenerationSettings settings =
            CreateSettings(
                generationSeed: 123456789UL,
                trunkSegmentCount: 6,
                trunkIrregularity: 0.25,
                trunkTwist: 0.75);

        TreeGenerationResult first =
            TreeGenerator.Generate(
                settings);
        TreeGenerationResult second =
            TreeGenerator.Generate(
                settings);

        AssertGenerationResultsEqual(
            first,
            second);
    }

    [Fact]
    public void IrregularityOffsetsIntermediateRingsWithoutMovingTopOrCanopy()
    {
        TreeGenerationResult regular =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkTaper: 0.0,
                    trunkIrregularity: 0.0));
        TreeGenerationResult irregular =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkTaper: 0.0,
                    trunkIrregularity: 0.25));

        GeneratedTreeBrush[] regularTrunk =
            GetTrunkParts(
                regular);
        GeneratedTreeBrush[] irregularTrunk =
            GetTrunkParts(
                irregular);

        bool anyIntermediateRingMoved = false;

        for (
            int segmentIndex = 0;
            segmentIndex < regularTrunk.Length - 1;
            segmentIndex++
        ) {
            Vector3d regularCenter =
                GetRingCenter(
                    regularTrunk[segmentIndex],
                    useTopRing: true);
            Vector3d irregularCenter =
                GetRingCenter(
                    irregularTrunk[segmentIndex],
                    useTopRing: true);

            if (
                !regularCenter.NearlyEquals(
                    irregularCenter,
                    CoordinateTolerance)
            ) {
                anyIntermediateRingMoved = true;
            }
        }

        Assert.True(
            anyIntermediateRingMoved);

        AssertNearlyEqual(
            GetRingCenter(
                regularTrunk[^1],
                useTopRing: true),
            GetRingCenter(
                irregularTrunk[^1],
                useTopRing: true));

        GeneratedTreeBrush[] regularCanopy =
            GetCanopyParts(
                regular);
        GeneratedTreeBrush[] irregularCanopy =
            GetCanopyParts(
                irregular);

        Assert.Equal(
            regularCanopy.Length,
            irregularCanopy.Length);

        for (
            int canopyIndex = 0;
            canopyIndex < regularCanopy.Length;
            canopyIndex++
        ) {
            Assert.Equal(
                regularCanopy[canopyIndex].Bounds,
                irregularCanopy[canopyIndex].Bounds);
        }
    }

    [Fact]
    public void TwistChangesIntermediateIrregularityPath()
    {
        TreeGenerationResult untwisted =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkIrregularity: 0.25,
                    trunkTwist: 0.0));
        TreeGenerationResult twisted =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkIrregularity: 0.25,
                    trunkTwist: 1.0));

        GeneratedTreeBrush[] untwistedTrunk =
            GetTrunkParts(
                untwisted);
        GeneratedTreeBrush[] twistedTrunk =
            GetTrunkParts(
                twisted);
        bool anyIntermediateRingChanged = false;

        for (
            int segmentIndex = 0;
            segmentIndex < untwistedTrunk.Length - 1;
            segmentIndex++
        ) {
            Vector3d untwistedCenter =
                GetRingCenter(
                    untwistedTrunk[segmentIndex],
                    useTopRing: true);
            Vector3d twistedCenter =
                GetRingCenter(
                    twistedTrunk[segmentIndex],
                    useTopRing: true);

            if (
                !untwistedCenter.NearlyEquals(
                    twistedCenter,
                    CoordinateTolerance)
            ) {
                anyIntermediateRingChanged = true;
            }
        }

        Assert.True(
            anyIntermediateRingChanged);

        AssertNearlyEqual(
            GetRingCenter(
                untwistedTrunk[^1],
                useTopRing: true),
            GetRingCenter(
                twistedTrunk[^1],
                useTopRing: true));
    }

    [Fact]
    public void MaximumIrregularityWithMaximumShapeControlsRemainsConvexAndConnected()
    {
        GeneratedTreeBrush[] trunkParts =
            GetTrunkParts(
                TreeGenerator.Generate(
                    CreateSettings(
                        trunkSegmentCount: 6,
                        trunkTaper: 0.75,
                        trunkLean: 0.30,
                        trunkBend: 0.20,
                        trunkBaseFlare: 1.0,
                        trunkIrregularity: 0.25,
                        trunkTwist: 1.0)));

        Assert.Equal(
            6,
            trunkParts.Length);

        Assert.All(
            trunkParts,
            part =>
            {
                BrushValidationResult validation =
                    ConvexBrushValidator.Validate(
                        part.Brush);

                Assert.True(
                    validation.IsValid);
            });

        for (
            int segmentIndex = 1;
            segmentIndex < trunkParts.Length;
            segmentIndex++
        ) {
            AssertRingsNearlyEqual(
                GetRingVertices(
                    trunkParts[segmentIndex - 1],
                    useTopRing: true),
                GetRingVertices(
                    trunkParts[segmentIndex],
                    useTopRing: false));
        }
    }

    private static void AssertGenerationResultsEqual(
        TreeGenerationResult expected,
        TreeGenerationResult actual)
    {
        Assert.Equal(
            expected.Parts.Count,
            actual.Parts.Count);
        Assert.Equal(
            expected.Bounds,
            actual.Bounds);

        for (
            int partIndex = 0;
            partIndex < expected.Parts.Count;
            partIndex++
        ) {
            GeneratedTreeBrush expectedPart =
                expected.Parts[partIndex];
            GeneratedTreeBrush actualPart =
                actual.Parts[partIndex];

            Assert.Equal(
                expectedPart.Role,
                actualPart.Role);
            Assert.Equal(
                expectedPart.CanopyLayerIndex,
                actualPart.CanopyLayerIndex);
            Assert.Equal(
                expectedPart.Bounds,
                actualPart.Bounds);
            Assert.Equal(
                expectedPart.Brush.Faces.Count,
                actualPart.Brush.Faces.Count);

            for (
                int faceIndex = 0;
                faceIndex < expectedPart.Brush.Faces.Count;
                faceIndex++
            ) {
                Assert.Equal(
                    expectedPart.Brush.Faces[faceIndex].Plane,
                    actualPart.Brush.Faces[faceIndex].Plane);
            }
        }
    }

    private static void AssertNearlyEqual(
        Vector3d expected,
        Vector3d actual)
    {
        Assert.True(
            expected.NearlyEquals(
                actual,
                CoordinateTolerance),
            $"Expected {actual} to be within {CoordinateTolerance:R} units of {expected}.");
    }

    private static void AssertRingsNearlyEqual(
        Vector3d[] expected,
        Vector3d[] actual)
    {
        Assert.Equal(
            expected.Length,
            actual.Length);

        bool[] matchedActualVertices =
            new bool[actual.Length];

        foreach (Vector3d expectedVertex in expected) {
            int matchingIndex = -1;

            for (
                int actualIndex = 0;
                actualIndex < actual.Length;
                actualIndex++
            ) {
                if (matchedActualVertices[actualIndex]) {
                    continue;
                }

                if (
                    expectedVertex.NearlyEquals(
                        actual[actualIndex],
                        CoordinateTolerance)
                ) {
                    matchingIndex =
                        actualIndex;

                    break;
                }
            }

            Assert.True(
                matchingIndex >= 0,
                $"No matching shared-ring vertex was found for {expectedVertex} within {CoordinateTolerance:R} units.");

            matchedActualVertices[matchingIndex] =
                true;
        }
    }

    private static GeneratedTreeBrush[] GetTrunkParts(
        TreeGenerationResult result)
    {
        return result.Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Trunk)
            .ToArray();
    }

    private static GeneratedTreeBrush[] GetCanopyParts(
        TreeGenerationResult result)
    {
        return result.Parts
            .Where(
                part =>
                    part.Role == TreeBrushRole.Canopy)
            .OrderBy(
                part =>
                    part.CanopyLayerIndex)
            .ToArray();
    }

    private static Vector3d GetRingCenter(
        GeneratedTreeBrush part,
        bool useTopRing)
    {
        Vector3d[] vertices =
            GetRingVertices(
                part,
                useTopRing);

        return new Vector3d(
            vertices.Average(
                vertex =>
                    vertex.X),
            vertices.Average(
                vertex =>
                    vertex.Y),
            vertices.Average(
                vertex =>
                    vertex.Z));
    }

    private static Vector3d[] GetRingVertices(
        GeneratedTreeBrush part,
        bool useTopRing)
    {
        BrushValidationResult validation =
            ConvexBrushValidator.Validate(
                part.Brush);

        Assert.True(
            validation.IsValid);

        Vector3d[] vertices =
            validation.Geometry!
                .Vertices
                .ToArray();
        double targetZ =
            useTopRing
                ? vertices.Max(
                    vertex =>
                        vertex.Z)
                : vertices.Min(
                    vertex =>
                        vertex.Z);

        return vertices
            .Where(
                vertex =>
                    Math.Abs(
                        vertex.Z -
                        targetZ) <=
                    CoordinateTolerance)
            .ToArray();
    }

    private static TreeGenerationSettings CreateSettings(
        ulong generationSeed = 42UL,
        int trunkSegmentCount =
            TreeGenerationSettings.DefaultTrunkSegmentCount,
        double trunkTaper =
            TreeGenerationSettings.DefaultTrunkTaper,
        double trunkLean =
            TreeGenerationSettings.DefaultTrunkLean,
        double trunkBend =
            TreeGenerationSettings.DefaultTrunkBend,
        double trunkBaseFlare =
            TreeGenerationSettings.DefaultTrunkBaseFlare,
        double trunkIrregularity =
            TreeGenerationSettings.DefaultTrunkIrregularity,
        double trunkTwist =
            TreeGenerationSettings.DefaultTrunkTwist)
    {
        return new TreeGenerationSettings(
            Vector3d.Zero,
            256.0,
            32.0,
            160.0,
            128.0,
            3,
            new GenerationSeed(
                generationSeed),
            GridSpacing.Eight,
            "WOOD",
            "LEAF",
            trunkSegmentCount,
            trunkTaper,
            trunkLean,
            trunkBend,
            trunkBaseFlare,
            TreeGenerationSettings.DefaultTrunkCrossSection,
            trunkIrregularity,
            trunkTwist);
    }
}
