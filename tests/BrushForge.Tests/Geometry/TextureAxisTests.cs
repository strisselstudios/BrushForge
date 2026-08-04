using BrushForge.Geometry.Planes;
using BrushForge.Geometry.Textures;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

public sealed class TextureAxisTests
{
    [Fact]
    public void TopFaceUsesValveStyleFloorAxes()
    {
        Valve220TextureAxes axes =
            TextureAxisGenerator.Generate(
                new Plane3d(
                    Vector3d.UnitZ,
                    64.0));

        Assert.True(
            axes.UAxis.Direction.NearlyEquals(
                Vector3d.UnitX));

        Assert.True(
            axes.VAxis.Direction.NearlyEquals(
                -Vector3d.UnitY));
    }

    [Fact]
    public void PositiveXWallUsesVerticalVAxis()
    {
        Valve220TextureAxes axes =
            TextureAxisGenerator.Generate(
                new Plane3d(
                    Vector3d.UnitX,
                    64.0));

        Assert.True(
            axes.UAxis.Direction.NearlyEquals(
                Vector3d.UnitY));

        Assert.True(
            axes.VAxis.Direction.NearlyEquals(
                -Vector3d.UnitZ));
    }

    [Fact]
    public void NinetyDegreeRotationRotatesGeneratedAxes()
    {
        Valve220TextureAxes axes =
            TextureAxisGenerator.Generate(
                new Plane3d(
                    Vector3d.UnitZ,
                    0.0),
                rotationDegrees: 90.0);

        Assert.True(
            axes.UAxis.Direction.NearlyEquals(
                -Vector3d.UnitY));

        Assert.True(
            axes.VAxis.Direction.NearlyEquals(
                -Vector3d.UnitX));

        Assert.Equal(90.0, axes.RotationDegrees);
    }

    [Fact]
    public void GeneratedOffsetsAndScaleArePreserved()
    {
        Valve220TextureAxes axes =
            TextureAxisGenerator.Generate(
                new Plane3d(
                    Vector3d.UnitZ,
                    0.0),
                textureScale: 0.5,
                uOffset: 8.0,
                vOffset: -4.0);

        Assert.Equal(0.5, axes.UAxis.Scale);
        Assert.Equal(0.5, axes.VAxis.Scale);
        Assert.Equal(8.0, axes.UAxis.Offset);
        Assert.Equal(-4.0, axes.VAxis.Offset);
    }

    [Fact]
    public void ParallelTextureAxesAreRejected()
    {
        TextureAxis uAxis = new(
            Vector3d.UnitX,
            0.0,
            1.0);

        TextureAxis vAxis = new(
            Vector3d.UnitX,
            0.0,
            1.0);

        Assert.Throws<ArgumentException>(
            () => new Valve220TextureAxes(
                uAxis,
                vAxis,
                0.0));
    }
}
