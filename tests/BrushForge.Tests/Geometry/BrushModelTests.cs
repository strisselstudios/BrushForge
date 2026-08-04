using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

public sealed class BrushModelTests
{
    [Fact]
    public void PlanePointsRetainOrientedPlane()
    {
        PlanePoints3d points = new(
            Vector3d.Zero,
            Vector3d.UnitX,
            Vector3d.UnitY);

        Assert.True(
            points.Plane.Normal.NearlyEquals(
                Vector3d.UnitZ));
    }

    [Fact]
    public void BrushFaceGeneratesTextureAxes()
    {
        BrushFace face = BrushFace.CreateWithGeneratedAxes(
            new PlanePoints3d(
                Vector3d.Zero,
                Vector3d.UnitX,
                Vector3d.UnitY),
            "STONE");

        Assert.Equal("STONE", face.TextureName);

        Assert.True(
            face.TextureAxes.UAxis.Direction.NearlyEquals(
                Vector3d.UnitX));
    }

    [Fact]
    public void BrushFaceRejectsInvalidTextureName()
    {
        Assert.Throws<ArgumentException>(
            () => BrushFace.CreateWithGeneratedAxes(
                new PlanePoints3d(
                    Vector3d.Zero,
                    Vector3d.UnitX,
                    Vector3d.UnitY),
                "BAD TEXTURE"));
    }

    [Fact]
    public void SixFaceBoxCreatesStructuralBrush()
    {
        ConvexBrush brush = CreateBoxBrush();

        Assert.Equal(6, brush.FaceCount);
        Assert.True(
            brush.ContainsPlane(
                brush.Faces[0].Plane));
    }

    [Fact]
    public void BrushRejectsDuplicateFacePlanes()
    {
        BrushFace face = BrushFace.CreateWithGeneratedAxes(
            new PlanePoints3d(
                Vector3d.Zero,
                Vector3d.UnitX,
                Vector3d.UnitY),
            "STONE");

        BrushFace second = BrushFace.CreateWithGeneratedAxes(
            new PlanePoints3d(
                Vector3d.Zero,
                Vector3d.UnitX,
                Vector3d.UnitY),
            "DIRT");

        Assert.Throws<ArgumentException>(
            () => new ConvexBrush(
            [
                face,
                second,
                CreateBoxBrush().Faces[0],
                CreateBoxBrush().Faces[1]
            ]));
    }

    private static ConvexBrush CreateBoxBrush()
    {
        const double maximum = 64.0;

        return new ConvexBrush(
        [
            CreateFace(
                new(0.0, 0.0, 0.0),
                new(0.0, maximum, 0.0),
                new(maximum, 0.0, 0.0)),

            CreateFace(
                new(0.0, 0.0, maximum),
                new(maximum, 0.0, maximum),
                new(0.0, maximum, maximum)),

            CreateFace(
                new(0.0, 0.0, 0.0),
                new(0.0, 0.0, maximum),
                new(0.0, maximum, 0.0)),

            CreateFace(
                new(maximum, 0.0, 0.0),
                new(maximum, maximum, 0.0),
                new(maximum, 0.0, maximum)),

            CreateFace(
                new(0.0, 0.0, 0.0),
                new(maximum, 0.0, 0.0),
                new(0.0, 0.0, maximum)),

            CreateFace(
                new(0.0, maximum, 0.0),
                new(0.0, maximum, maximum),
                new(maximum, maximum, 0.0))
        ]);
    }

    private static BrushFace CreateFace(
        Vector3d first,
        Vector3d second,
        Vector3d third)
    {
        return BrushFace.CreateWithGeneratedAxes(
            new PlanePoints3d(
                first,
                second,
                third),
            "STONE");
    }
}
