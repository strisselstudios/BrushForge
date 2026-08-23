using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;
using BrushForge.Generation.Foliage.Input;

namespace BrushForge.Tests.Generation;

public sealed class TrunkDeformationControlTests
{
    private const double CoordinateTolerance = 1e-8;

    [Fact]
    public void SettingsUseStraightTrunkDeformationDefaults()
    {
        TreeGenerationSettings settings =
            CreateSettings();

        Assert.Equal(
            TreeGenerationSettings.DefaultTrunkLean,
            settings.TrunkLean);
        Assert.Equal(
            TreeGenerationSettings.DefaultTrunkBend,
            settings.TrunkBend);
    }

    [Fact]
    public void SettingsRejectUnsupportedTrunkDeformationValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkLean: -0.01));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkLean: 0.31));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkBend: -0.01));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkBend: 0.21));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateSettings(
                    trunkLean: double.NaN));
    }

    [Fact]
    public void ParserReadsExplicitTrunkDeformationControls()
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
                    "25",
                    "30",
                    "20"));

        Assert.Equal(0.30, settings.TrunkLean);
        Assert.Equal(0.20, settings.TrunkBend);
    }

    [Fact]
    public void SameSeedAndSettingsReproduceDeformedGeometry()
    {
        TreeGenerationSettings settings =
            CreateSettings(
                trunkSegmentCount: 6,
                trunkTaper: 0.50,
                trunkLean: 0.30,
                trunkBend: 0.20);

        TreeGenerationResult first =
            TreeGenerator.Generate(
                settings);
        TreeGenerationResult second =
            TreeGenerator.Generate(
                settings);

        Assert.Equal(
            first.Parts.Count,
            second.Parts.Count);

        for (
            int partIndex = 0;
            partIndex < first.Parts.Count;
            partIndex++
        ) {
            GeneratedTreeBrush firstPart =
                first.Parts[partIndex];
            GeneratedTreeBrush secondPart =
                second.Parts[partIndex];

            Assert.Equal(
                firstPart.Role,
                secondPart.Role);
            Assert.Equal(
                firstPart.CanopyLayerIndex,
                secondPart.CanopyLayerIndex);
            Assert.Equal(
                firstPart.Bounds,
                secondPart.Bounds);
            Assert.Equal(
                firstPart.Brush.FaceCount,
                secondPart.Brush.FaceCount);

            for (
                int faceIndex = 0;
                faceIndex < firstPart.Brush.FaceCount;
                faceIndex++
            ) {
                Assert.Equal(
                    firstPart.Brush.Faces[faceIndex].PlanePoints,
                    secondPart.Brush.Faces[faceIndex].PlanePoints);
            }
        }
    }

    [Fact]
    public void ZeroDeformationKeepsEveryTrunkRingCentered()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6));

        GeneratedTreeBrush[] trunkParts =
            GetTrunkParts(
                result);

        Assert.All(
            trunkParts,
            part =>
            {
                (Vector3d Bottom, Vector3d Top) =
                    MeasureRingCenters(
                        part);

                AssertNearlyZero(Bottom.X);
                AssertNearlyZero(Bottom.Y);
                AssertNearlyZero(Top.X);
                AssertNearlyZero(Top.Y);
            });
    }

    [Fact]
    public void LeanMovesTrunkTopAndTranslatesCanopyWithIt()
    {
        TreeGenerationResult straight =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6));
        TreeGenerationResult leaned =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkLean: 0.30));

        Vector3d straightTop =
            MeasureRingCenters(
                GetTrunkParts(
                    straight)[^1])
                .Top;
        Vector3d leanedTop =
            MeasureRingCenters(
                GetTrunkParts(
                    leaned)[^1])
                .Top;
        Vector3d displacement =
            leanedTop -
            straightTop;

        Assert.True(
            Math.Abs(displacement.X) > CoordinateTolerance ||
            Math.Abs(displacement.Y) > CoordinateTolerance);

        GeneratedTreeBrush[] straightCanopy =
            GetCanopyParts(
                straight);
        GeneratedTreeBrush[] leanedCanopy =
            GetCanopyParts(
                leaned);

        Assert.Equal(
            straightCanopy.Length,
            leanedCanopy.Length);

        for (
            int index = 0;
            index < straightCanopy.Length;
            index++
        ) {
            Vector3d canopyDisplacement =
                leanedCanopy[index].Bounds.Center -
                straightCanopy[index].Bounds.Center;

            Assert.True(
                canopyDisplacement.NearlyEquals(
                    displacement));
        }
    }

    [Fact]
    public void BendOffsetsIntermediateRingsWithoutMovingTheTop()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkBend: 0.20));

        GeneratedTreeBrush[] trunkParts =
            GetTrunkParts(
                result);
        Vector3d topCenter =
            MeasureRingCenters(
                trunkParts[^1])
                .Top;

        AssertNearlyZero(topCenter.X);
        AssertNearlyZero(topCenter.Y);

        Assert.Contains(
            trunkParts,
            part =>
            {
                (Vector3d Bottom, Vector3d Top) =
                    MeasureRingCenters(
                        part);

                return
                    Math.Abs(Bottom.X) > CoordinateTolerance ||
                    Math.Abs(Bottom.Y) > CoordinateTolerance ||
                    Math.Abs(Top.X) > CoordinateTolerance ||
                    Math.Abs(Top.Y) > CoordinateTolerance;
            });
    }

    [Fact]
    public void MaximumDeformationKeepsSegmentsConvexAndConnected()
    {
        TreeGenerationResult result =
            TreeGenerator.Generate(
                CreateSettings(
                    trunkSegmentCount: 6,
                    trunkTaper: 0.75,
                    trunkLean: 0.30,
                    trunkBend: 0.20));

        GeneratedTreeBrush[] trunkParts =
            GetTrunkParts(
                result);

        Assert.Equal(6, trunkParts.Length);

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
            Vector3d[] previousTop =
                GetRingVertices(
                    trunkParts[segmentIndex - 1],
                    useTopRing: true);
            Vector3d[] currentBottom =
                GetRingVertices(
                    trunkParts[segmentIndex],
                    useTopRing: false);

            AssertRingsNearlyEqual(
                previousTop,
                currentBottom);
        }
    }

    private static void AssertNearlyZero(double value)
    {
        Assert.True(
            Math.Abs(value) <= CoordinateTolerance,
            $"Expected {value:R} to be within {CoordinateTolerance:R} of zero.");
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

    private static (Vector3d Bottom, Vector3d Top) MeasureRingCenters(
        GeneratedTreeBrush part)
    {
        Vector3d[] bottom =
            GetRingVertices(
                part,
                useTopRing: false);
        Vector3d[] top =
            GetRingVertices(
                part,
                useTopRing: true);

        return (
            Average(bottom),
            Average(top));
    }

    private static Vector3d[] GetRingVertices(
        GeneratedTreeBrush part,
        bool useTopRing)
    {
        BrushValidationResult validation =
            ConvexBrushValidator.Validate(
                part.Brush);

        Assert.True(validation.IsValid);

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
                        targetZ) <= CoordinateTolerance)
            .OrderBy(
                vertex =>
                    vertex.X)
            .ThenBy(
                vertex =>
                    vertex.Y)
            .ToArray();
    }

    private static Vector3d Average(
        IReadOnlyList<Vector3d> vertices)
    {
        Assert.NotEmpty(vertices);

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

    private static TreeGenerationSettings CreateSettings(
        int trunkSegmentCount =
            TreeGenerationSettings.DefaultTrunkSegmentCount,
        double trunkTaper =
            TreeGenerationSettings.DefaultTrunkTaper,
        double trunkLean =
            TreeGenerationSettings.DefaultTrunkLean,
        double trunkBend =
            TreeGenerationSettings.DefaultTrunkBend)
    {
        return new TreeGenerationSettings(
            Vector3d.Zero,
            256.0,
            32.0,
            160.0,
            128.0,
            3,
            new GenerationSeed(42UL),
            GridSpacing.Eight,
            "WOOD",
            "LEAF",
            trunkSegmentCount,
            trunkTaper,
            trunkLean,
            trunkBend);
    }
}
