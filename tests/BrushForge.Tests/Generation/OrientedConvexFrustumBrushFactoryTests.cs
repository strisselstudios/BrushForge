using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Geometry;

namespace BrushForge.Tests.Generation;

public sealed class OrientedConvexFrustumBrushFactoryTests
{
    [Fact]
    public void CreateBuildsValidHorizontalOctagonalPrism()
    {
        ConvexBrush brush =
            OrientedConvexFrustumBrushFactory.Create(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    0.0,
                    0.0),
                startApothem: 8.0,
                endApothem: 8.0,
                sideCount: 8,
                textureName: "BARK");

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(brush);

        Assert.True(validation.IsValid);
        Assert.Equal(10, brush.FaceCount);
        Assert.Equal(16, validation.Geometry!.Vertices.Count);
        Assert.True(validation.Geometry.Volume > 0.0);
    }

    [Fact]
    public void CreateBuildsValidDiagonalHexagonalTaperedFrustum()
    {
        ConvexBrush brush =
            OrientedConvexFrustumBrushFactory.Create(
                new Vector3d(
                    -16.0,
                    8.0,
                    24.0),
                new Vector3d(
                    48.0,
                    72.0,
                    56.0),
                startApothem: 12.0,
                endApothem: 6.0,
                sideCount: 6,
                textureName: "BARK");

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(brush);

        Assert.True(validation.IsValid);
        Assert.Equal(8, brush.FaceCount);
        Assert.Equal(12, validation.Geometry!.Vertices.Count);
        Assert.All(
            brush.Faces,
            face =>
                Assert.Equal(
                    "BARK",
                    face.TextureName));
    }

    [Fact]
    public void FourSidedModeMatchesSquareFactoryEnvelopeAndVolume()
    {
        Vector3d start =
            new(
                -16.0,
                8.0,
                24.0);
        Vector3d end =
            new(
                48.0,
                72.0,
                56.0);
        ConvexBrush square =
            OrientedSquareFrustumBrushFactory.Create(
                start,
                end,
                startHalfExtent: 12.0,
                endHalfExtent: 6.0,
                textureName: "BARK");
        ConvexBrush regular =
            OrientedConvexFrustumBrushFactory.Create(
                start,
                end,
                startApothem: 12.0,
                endApothem: 6.0,
                sideCount: 4,
                textureName: "BARK");
        BrushValidationResult squareValidation =
            ConvexBrushValidator.Validate(square);
        BrushValidationResult regularValidation =
            ConvexBrushValidator.Validate(regular);

        Assert.True(squareValidation.IsValid);
        Assert.True(regularValidation.IsValid);
        Assert.True(
            squareValidation.Geometry!.Bounds.Minimum.NearlyEquals(
                regularValidation.Geometry!.Bounds.Minimum,
                1e-8));
        Assert.True(
            squareValidation.Geometry.Bounds.Maximum.NearlyEquals(
                regularValidation.Geometry.Bounds.Maximum,
                1e-8));
        Assert.Equal(
            squareValidation.Geometry.Volume,
            regularValidation.Geometry.Volume,
            precision: 6);
    }

    [Fact]
    public void CreateRejectsUnsupportedSideCounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                OrientedConvexFrustumBrushFactory.Create(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        0.0,
                        0.0),
                    startApothem: 8.0,
                    endApothem: 8.0,
                    sideCount:
                        OrientedConvexFrustumBrushFactory.MinimumSideCount -
                        1,
                    textureName: "BARK"));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                OrientedConvexFrustumBrushFactory.Create(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        0.0,
                        0.0),
                    startApothem: 8.0,
                    endApothem: 8.0,
                    sideCount:
                        OrientedConvexFrustumBrushFactory.MaximumSideCount +
                        1,
                    textureName: "BARK"));
    }
}
