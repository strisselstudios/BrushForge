using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Geometry;

namespace BrushForge.Tests.Generation;

public sealed class VerticalConvexFrustumBrushFactoryTests
{
    [Fact]
    public void CreateBuildsValidSquarePrism()
    {
        ConvexBrush brush =
            VerticalConvexFrustumBrushFactory.Create(
                CreateSquareRing(
                    centerX: 0.0,
                    centerY: 0.0,
                    z: 0.0,
                    halfExtent: 16.0),
                CreateSquareRing(
                    centerX: 0.0,
                    centerY: 0.0,
                    z: 64.0,
                    halfExtent: 16.0),
                "WOOD");

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(brush);

        Assert.True(validation.IsValid);
        Assert.Equal(6, brush.FaceCount);
        Assert.Equal(8, validation.Geometry!.Vertices.Count);
        Assert.Equal(65_536.0, validation.Geometry.Volume);
    }

    [Fact]
    public void CreateBuildsValidTaperedOctagonalFrustum()
    {
        ConvexBrush brush =
            VerticalConvexFrustumBrushFactory.Create(
                CreateOctagonalRing(
                    centerX: 0.0,
                    centerY: 0.0,
                    z: 0.0,
                    halfExtent: 32.0,
                    cornerInset: 8.0),
                CreateOctagonalRing(
                    centerX: 8.0,
                    centerY: 0.0,
                    z: 64.0,
                    halfExtent: 24.0,
                    cornerInset: 8.0),
                "BARK");

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(brush);

        Assert.True(validation.IsValid);
        Assert.Equal(10, brush.FaceCount);
        Assert.Equal(16, validation.Geometry!.Vertices.Count);
        Assert.Equal(-32.0, validation.Geometry.Bounds.Minimum.X);
        Assert.Equal(32.0, validation.Geometry.Bounds.Maximum.X);
        Assert.Equal(64.0, validation.Geometry.Bounds.Maximum.Z);
        Assert.All(
            brush.Faces,
            face =>
                Assert.Equal(
                    "BARK",
                    face.TextureName));
    }

    [Fact]
    public void CreateSupportsOffsetTopRingForBentSegments()
    {
        ConvexBrush brush =
            VerticalConvexFrustumBrushFactory.Create(
                CreateSquareRing(
                    centerX: 0.0,
                    centerY: 0.0,
                    z: 0.0,
                    halfExtent: 16.0),
                CreateSquareRing(
                    centerX: 8.0,
                    centerY: -8.0,
                    z: 64.0,
                    halfExtent: 16.0),
                "WOOD");

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(brush);

        Assert.True(validation.IsValid);
        Assert.Equal(
            new Vector3d(
                4.0,
                -4.0,
                32.0),
            validation.Geometry!.Centroid);
    }

    [Fact]
    public void CreateRejectsClockwiseRingOrder()
    {
        Vector3d[] topRing =
            CreateSquareRing(
                centerX: 0.0,
                centerY: 0.0,
                z: 64.0,
                halfExtent: 16.0)
                .Reverse()
                .ToArray();

        Assert.Throws<ArgumentException>(
            () =>
                VerticalConvexFrustumBrushFactory.Create(
                    CreateSquareRing(
                        centerX: 0.0,
                        centerY: 0.0,
                        z: 0.0,
                        halfExtent: 16.0),
                    topRing,
                    "WOOD"));
    }

    [Fact]
    public void CreateRejectsNonHorizontalRing()
    {
        Vector3d[] topRing =
            CreateSquareRing(
                centerX: 0.0,
                centerY: 0.0,
                z: 64.0,
                halfExtent: 16.0);
        topRing[2] =
            topRing[2] with
            {
                Z = 72.0
            };

        Assert.Throws<ArgumentException>(
            () =>
                VerticalConvexFrustumBrushFactory.Create(
                    CreateSquareRing(
                        centerX: 0.0,
                        centerY: 0.0,
                        z: 0.0,
                        halfExtent: 16.0),
                    topRing,
                    "WOOD"));
    }

    [Fact]
    public void CreateRejectsNonPlanarSideCorrespondence()
    {
        Vector3d[] topRing =
            CreateSquareRing(
                centerX: 0.0,
                centerY: 0.0,
                z: 64.0,
                halfExtent: 16.0);
        topRing[1] =
            topRing[1] with
            {
                X = 24.0
            };

        Assert.Throws<ArgumentException>(
            () =>
                VerticalConvexFrustumBrushFactory.Create(
                    CreateSquareRing(
                        centerX: 0.0,
                        centerY: 0.0,
                        z: 0.0,
                        halfExtent: 16.0),
                    topRing,
                    "WOOD"));
    }

    private static Vector3d[] CreateSquareRing(
        double centerX,
        double centerY,
        double z,
        double halfExtent)
    {
        return
        [
            new Vector3d(
                centerX - halfExtent,
                centerY - halfExtent,
                z),
            new Vector3d(
                centerX + halfExtent,
                centerY - halfExtent,
                z),
            new Vector3d(
                centerX + halfExtent,
                centerY + halfExtent,
                z),
            new Vector3d(
                centerX - halfExtent,
                centerY + halfExtent,
                z)
        ];
    }

    private static Vector3d[] CreateOctagonalRing(
        double centerX,
        double centerY,
        double z,
        double halfExtent,
        double cornerInset)
    {
        return
        [
            new Vector3d(
                centerX - halfExtent + cornerInset,
                centerY - halfExtent,
                z),
            new Vector3d(
                centerX + halfExtent - cornerInset,
                centerY - halfExtent,
                z),
            new Vector3d(
                centerX + halfExtent,
                centerY - halfExtent + cornerInset,
                z),
            new Vector3d(
                centerX + halfExtent,
                centerY + halfExtent - cornerInset,
                z),
            new Vector3d(
                centerX + halfExtent - cornerInset,
                centerY + halfExtent,
                z),
            new Vector3d(
                centerX - halfExtent + cornerInset,
                centerY + halfExtent,
                z),
            new Vector3d(
                centerX - halfExtent,
                centerY + halfExtent - cornerInset,
                z),
            new Vector3d(
                centerX - halfExtent,
                centerY - halfExtent + cornerInset,
                z)
        ];
    }
}
