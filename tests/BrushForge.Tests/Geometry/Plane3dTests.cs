using BrushForge.Geometry.Planes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

public sealed class Plane3dTests
{
    [Fact]
    public void FromPointsPreservesRightHandedOrientation()
    {
        Plane3d plane = Plane3d.FromPoints(
            Vector3d.Zero,
            Vector3d.UnitX,
            Vector3d.UnitY);

        Assert.True(
            plane.Normal.NearlyEquals(
                Vector3d.UnitZ));

        Assert.Equal(0.0, plane.Distance);
    }

    [Fact]
    public void ConstructorNormalizesNormalAndDistance()
    {
        Plane3d plane = new(
            new Vector3d(0.0, 0.0, 2.0),
            16.0);

        Assert.Equal(Vector3d.UnitZ, plane.Normal);
        Assert.Equal(8.0, plane.Distance);
    }

    [Fact]
    public void SignedDistanceDistinguishesPlaneSides()
    {
        Plane3d plane = new(
            Vector3d.UnitZ,
            8.0);

        Assert.Equal(
            4.0,
            plane.SignedDistanceTo(
                new Vector3d(0.0, 0.0, 12.0)));

        Assert.Equal(
            -4.0,
            plane.SignedDistanceTo(
                new Vector3d(0.0, 0.0, 4.0)));
    }

    [Fact]
    public void ProjectionMovesPointOntoPlane()
    {
        Plane3d plane = new(
            Vector3d.UnitZ,
            8.0);

        Vector3d projected = plane.ProjectPoint(
            new Vector3d(4.0, 6.0, 20.0));

        Assert.Equal(
            new Vector3d(4.0, 6.0, 8.0),
            projected);

        Assert.Equal(
            0.0,
            plane.SignedDistanceTo(projected));
    }

    [Fact]
    public void FlipReversesPlaneOrientation()
    {
        Plane3d original = new(
            Vector3d.UnitY,
            32.0);

        Plane3d flipped = original.Flip();

        Assert.Equal(-Vector3d.UnitY, flipped.Normal);
        Assert.Equal(-32.0, flipped.Distance);
    }

    [Fact]
    public void CollinearPointsAreRejected()
    {
        Assert.Throws<ArgumentException>(
            () => Plane3d.FromPoints(
                Vector3d.Zero,
                Vector3d.One,
                Vector3d.One * 2.0));
    }
}
