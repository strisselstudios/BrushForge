using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Geometry;

namespace BrushForge.Tests.Generation;

public sealed class OrientedSquareFrustumBrushFactoryTests
{
    [Fact]
    public void CreateBuildsValidHorizontalSquarePrism()
    {
        ConvexBrush brush =
            OrientedSquareFrustumBrushFactory.Create(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    0.0,
                    0.0),
                startHalfExtent: 8.0,
                endHalfExtent: 8.0,
                textureName: "BARK");

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(brush);

        Assert.True(validation.IsValid);
        Assert.Equal(6, brush.FaceCount);
        Assert.Equal(8, validation.Geometry!.Vertices.Count);
        Assert.Equal(16_384.0, validation.Geometry.Volume, precision: 6);
        Assert.Equal(0.0, validation.Geometry.Bounds.Minimum.X, precision: 6);
        Assert.Equal(64.0, validation.Geometry.Bounds.Maximum.X, precision: 6);
    }

    [Fact]
    public void CreateBuildsValidVerticalSquarePrism()
    {
        ConvexBrush brush =
            OrientedSquareFrustumBrushFactory.Create(
                Vector3d.Zero,
                new Vector3d(
                    0.0,
                    0.0,
                    64.0),
                startHalfExtent: 8.0,
                endHalfExtent: 8.0,
                textureName: "BARK");

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(brush);

        Assert.True(validation.IsValid);
        Assert.Equal(6, brush.FaceCount);
        Assert.Equal(8, validation.Geometry!.Vertices.Count);
        Assert.Equal(16_384.0, validation.Geometry.Volume, precision: 6);
    }

    [Fact]
    public void CreateBuildsValidDiagonalTaperedFrustum()
    {
        ConvexBrush brush =
            OrientedSquareFrustumBrushFactory.Create(
                new Vector3d(
                    -16.0,
                    8.0,
                    24.0),
                new Vector3d(
                    48.0,
                    72.0,
                    56.0),
                startHalfExtent: 12.0,
                endHalfExtent: 6.0,
                textureName: "BARK");

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(brush);

        Assert.True(validation.IsValid);
        Assert.Equal(6, brush.FaceCount);
        Assert.Equal(8, validation.Geometry!.Vertices.Count);
        Assert.True(validation.Geometry.Volume > 0.0);
        Assert.All(
            brush.Faces,
            face =>
                Assert.Equal(
                    "BARK",
                    face.TextureName));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void CreateRejectsInvalidHalfExtent(
        double halfExtent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                OrientedSquareFrustumBrushFactory.Create(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        0.0,
                        0.0),
                    startHalfExtent: halfExtent,
                    endHalfExtent: 8.0,
                    textureName: "BARK"));
    }

    [Fact]
    public void CreateRejectsCoincidentEndpoints()
    {
        Assert.Throws<ArgumentException>(
            () =>
                OrientedSquareFrustumBrushFactory.Create(
                    Vector3d.Zero,
                    Vector3d.Zero,
                    startHalfExtent: 8.0,
                    endHalfExtent: 8.0,
                    textureName: "BARK"));
    }

    [Fact]
    public void CreateRejectsNonFiniteEndpoints()
    {
        Vector3d nonFinite = new(
            double.PositiveInfinity,
            0.0,
            0.0);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                OrientedSquareFrustumBrushFactory.Create(
                    nonFinite,
                    new Vector3d(
                        64.0,
                        0.0,
                        0.0),
                    startHalfExtent: 8.0,
                    endHalfExtent: 8.0,
                    textureName: "BARK"));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                OrientedSquareFrustumBrushFactory.Create(
                    Vector3d.Zero,
                    nonFinite,
                    startHalfExtent: 8.0,
                    endHalfExtent: 8.0,
                    textureName: "BARK"));
    }
}
