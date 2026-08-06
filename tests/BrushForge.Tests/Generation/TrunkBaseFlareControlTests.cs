using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Input;

namespace BrushForge.Tests.Generation;

public sealed class TrunkBaseFlareControlTests
{
    private const double CoordinateTolerance = 1e-8;

    [Fact]
    public void SettingsUseDocumentedTrunkBaseFlareDefault()
    {
        TreeGenerationSettings settings =
            CreateSettings();

        Assert.Equal(
            TreeGenerationSettings.DefaultTrunkBaseFlare,
            settings.TrunkBaseFlare);
        Assert.Equal(
            0.0,
            TreeGenerationSettings.MinimumTrunkBaseFlare);
        Assert.Equal(
            1.0,
            TreeGenerationSettings.MaximumTrunkBaseFlare);
    }

    [Fact]
    public void SettingsRejectUnsupportedTrunkBaseFlareValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkBaseFlare: -0.01));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkBaseFlare: 1.01));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkBaseFlare: double.NaN));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkBaseFlare:
                        double.PositiveInfinity));
    }

    [Fact]
    public void ParserReadsExplicitTrunkBaseFlareControl()
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
                    "6",
                    "50",
                    "30",
                    "20",
                    "100"));

        Assert.Equal(
            1.0,
            settings.TrunkBaseFlare);
    }

    [Fact]
    public void ZeroBaseFlarePreservesExistingTrunkFootprint()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkBaseFlare: 0.0));

        GeneratedTreeBrush firstTrunkPart =
            GetTrunkParts(
                result)[0];
        Vector3d[] bottomRing =
            GetRingVertices(
                firstTrunkPart,
                useTopRing: false);
        Vector3d bottomCenter =
            Average(
                bottomRing);

        AssertNearlyEqual(
            32.0,
            MeasureRingWidth(
                bottomRing));
        AssertNearlyEqual(
            32.0,
            MeasureRingDepth(
                bottomRing));
        AssertNearlyZero(
            bottomCenter.X);
        AssertNearlyZero(
            bottomCenter.Y);
    }

    [Fact]
    public void BaseFlareWidensOnlyBottomRingAsymmetrically()
    {
        TreeGenerationResult straight =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkBaseFlare: 0.0));
        TreeGenerationResult flared =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkBaseFlare: 1.0));

        GeneratedTreeBrush[] straightTrunk =
            GetTrunkParts(
                straight);
        GeneratedTreeBrush[] flaredTrunk =
            GetTrunkParts(
                flared);
        Vector3d[] straightBottom =
            GetRingVertices(
                straightTrunk[0],
                useTopRing: false);
        Vector3d[] flaredBottom =
            GetRingVertices(
                flaredTrunk[0],
                useTopRing: false);
        Vector3d bottomDisplacement =
            Average(
                flaredBottom) -
            Average(
                straightBottom);

        AssertNearlyEqual(
            64.0,
            MeasureRingWidth(
                flaredBottom));
        AssertNearlyEqual(
            64.0,
            MeasureRingDepth(
                flaredBottom));
        Assert.True(
            (
                Math.Abs(bottomDisplacement.X) >
                    CoordinateTolerance &&
                Math.Abs(bottomDisplacement.Y) <=
                    CoordinateTolerance
            ) ||
            (
                Math.Abs(bottomDisplacement.Y) >
                    CoordinateTolerance &&
                Math.Abs(bottomDisplacement.X) <=
                    CoordinateTolerance
            ));
        AssertNearlyEqual(
            16.0,
            Math.Max(
                Math.Abs(bottomDisplacement.X),
                Math.Abs(bottomDisplacement.Y)));

        AssertRingsNearlyEqual(
            GetRingVertices(
                straightTrunk[0],
                useTopRing: true),
            GetRingVertices(
                flaredTrunk[0],
                useTopRing: true));

        for (
            int segmentIndex = 1;
            segmentIndex < straightTrunk.Length;
            segmentIndex++
        ) {
            Assert.Equal(
                straightTrunk[segmentIndex].Bounds,
                flaredTrunk[segmentIndex].Bounds);
            Assert.Equal(
                straightTrunk[segmentIndex].Brush.Faces
                    .Select(
                        face =>
                            face.PlanePoints)
                    .ToArray(),
                flaredTrunk[segmentIndex].Brush.Faces
                    .Select(
                        face =>
                            face.PlanePoints)
                    .ToArray());
        }
    }

    [Fact]
    public void SameSeedReproducesBaseFlareDirection()
    {
        TreeGenerationSettings settings =
            CreateSettings(
                generationSeed: 987654321UL,
                trunkBaseFlare: 0.75);

        TreeGenerationResult first =
            TreeGenerator.Generate(
                settings);
        TreeGenerationResult second =
            TreeGenerator.Generate(
                settings);

        Vector3d firstCenter =
            Average(
                GetRingVertices(
                    GetTrunkParts(
                        first)[0],
                    useTopRing: false));
        Vector3d secondCenter =
            Average(
                GetRingVertices(
                    GetTrunkParts(
                        second)[0],
                    useTopRing: false));

        Assert.True(
            firstCenter.NearlyEquals(
                secondCenter,
                CoordinateTolerance));
        Assert.True(
            Math.Abs(firstCenter.X) >
                CoordinateTolerance ||
            Math.Abs(firstCenter.Y) >
                CoordinateTolerance);
    }

    [Fact]
    public void BaseFlareDoesNotMoveTrunkTopOrCanopy()
    {
        TreeGenerationResult unflared =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkTaper: 0.50,
                    trunkLean: 0.30,
                    trunkBend: 0.20,
                    trunkBaseFlare: 0.0));
        TreeGenerationResult flared =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkTaper: 0.50,
                    trunkLean: 0.30,
                    trunkBend: 0.20,
                    trunkBaseFlare: 1.0));

        GeneratedTreeBrush[] unflaredTrunk =
            GetTrunkParts(
                unflared);
        GeneratedTreeBrush[] flaredTrunk =
            GetTrunkParts(
                flared);

        AssertRingsNearlyEqual(
            GetRingVertices(
                unflaredTrunk[^1],
                useTopRing: true),
            GetRingVertices(
                flaredTrunk[^1],
                useTopRing: true));

        GeneratedTreeBrush[] unflaredCanopy =
            GetCanopyParts(
                unflared);
        GeneratedTreeBrush[] flaredCanopy =
            GetCanopyParts(
                flared);

        Assert.Equal(
            unflaredCanopy.Length,
            flaredCanopy.Length);

        for (
            int canopyIndex = 0;
            canopyIndex < unflaredCanopy.Length;
            canopyIndex++
        ) {
            Assert.Equal(
                unflaredCanopy[canopyIndex].Bounds,
                flaredCanopy[canopyIndex].Bounds);
        }
    }

    [Fact]
    public void MaximumBaseFlareWithMaximumShapeControlsRemainsConvexAndConnected()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkTaper: 0.75,
                    trunkLean: 0.30,
                    trunkBend: 0.20,
                    trunkBaseFlare: 1.0));

        GeneratedTreeBrush[] trunkParts =
            GetTrunkParts(
                result);

        Assert.Equal(
            6,
            trunkParts.Length);

        Assert.All(
            trunkParts,
            part =>
                Assert.True(
                    ConvexBrushValidator.Validate(
                        part.Brush)
                        .IsValid));

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

    private static void AssertNearlyZero(
        double value)
    {
        AssertNearlyEqual(
            0.0,
            value);
    }

    private static void AssertNearlyEqual(
        double expected,
        double actual)
    {
        Assert.True(
            Math.Abs(
                expected -
                actual) <=
            CoordinateTolerance,
            $"Expected {actual:R} to be within {CoordinateTolerance:R} of {expected:R}.");
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
                $"No matching ring vertex was found for {expectedVertex} within {CoordinateTolerance:R} units.");

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

    private static Vector3d Average(
        Vector3d[] vertices)
    {
        Assert.NotEmpty(
            vertices);

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

    private static double MeasureRingWidth(
        Vector3d[] vertices)
    {
        return
            vertices.Max(
                vertex =>
                    vertex.X) -
            vertices.Min(
                vertex =>
                    vertex.X);
    }

    private static double MeasureRingDepth(
        Vector3d[] vertices)
    {
        return
            vertices.Max(
                vertex =>
                    vertex.Y) -
            vertices.Min(
                vertex =>
                    vertex.Y);
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
            TreeGenerationSettings.DefaultTrunkBaseFlare)
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
            trunkBaseFlare);
    }
}
