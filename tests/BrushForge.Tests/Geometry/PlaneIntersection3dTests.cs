using BrushForge.Geometry.Intersections;
using BrushForge.Geometry.Planes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

public sealed class PlaneIntersection3dTests
{
    [Fact]
    public void OrthogonalPlanesIntersectAtExpectedPoint()
    {
        bool intersects =
            PlaneIntersection3d.TryIntersect(
                new Plane3d(
                    Vector3d.UnitX,
                    8.0),
                new Plane3d(
                    Vector3d.UnitY,
                    16.0),
                new Plane3d(
                    Vector3d.UnitZ,
                    32.0),
                out Vector3d intersection);

        Assert.True(intersects);

        Assert.True(
            intersection.NearlyEquals(
                new Vector3d(
                    8.0,
                    16.0,
                    32.0)));
    }

    [Fact]
    public void ParallelPlanesDoNotProduceIntersection()
    {
        bool intersects =
            PlaneIntersection3d.TryIntersect(
                new Plane3d(
                    Vector3d.UnitX,
                    0.0),
                new Plane3d(
                    Vector3d.UnitX,
                    32.0),
                new Plane3d(
                    Vector3d.UnitZ,
                    0.0),
                out _);

        Assert.False(intersects);
    }

    [Fact]
    public void NegativeDeterminantToleranceIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PlaneIntersection3d.TryIntersect(
                new Plane3d(
                    Vector3d.UnitX,
                    0.0),
                new Plane3d(
                    Vector3d.UnitY,
                    0.0),
                new Plane3d(
                    Vector3d.UnitZ,
                    0.0),
                out _,
                determinantTolerance: -1.0));
    }
}
