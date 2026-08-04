using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

public sealed class Bounds3dTests
{
    [Fact]
    public void FromPointsCalculatesAllExtents()
    {
        Bounds3d bounds = Bounds3d.FromPoints(
        [
            new Vector3d(8.0, -4.0, 12.0),
            new Vector3d(-2.0, 6.0, 32.0),
            new Vector3d(4.0, 1.0, -8.0)
        ]);

        Assert.Equal(
            new Vector3d(-2.0, -4.0, -8.0),
            bounds.Minimum);

        Assert.Equal(
            new Vector3d(8.0, 6.0, 32.0),
            bounds.Maximum);
    }

    [Fact]
    public void SizeAndCenterAreCalculatedCorrectly()
    {
        Bounds3d bounds = new(
            new Vector3d(-16.0, -8.0, 0.0),
            new Vector3d(16.0, 24.0, 64.0));

        Assert.Equal(
            new Vector3d(32.0, 32.0, 64.0),
            bounds.Size);

        Assert.Equal(
            new Vector3d(0.0, 8.0, 32.0),
            bounds.Center);
    }

    [Fact]
    public void ContainsIncludesBoundaryPoints()
    {
        Bounds3d bounds = new(
            Vector3d.Zero,
            new Vector3d(64.0, 64.0, 64.0));

        Assert.True(bounds.Contains(Vector3d.Zero));
        Assert.True(
            bounds.Contains(
                new Vector3d(64.0, 64.0, 64.0)));

        Assert.False(
            bounds.Contains(
                new Vector3d(65.0, 64.0, 64.0)));
    }

    [Fact]
    public void IntersectsDetectsTouchingBounds()
    {
        Bounds3d first = new(
            Vector3d.Zero,
            new Vector3d(16.0, 16.0, 16.0));

        Bounds3d second = new(
            new Vector3d(16.0, 4.0, 4.0),
            new Vector3d(32.0, 12.0, 12.0));

        Assert.True(first.Intersects(second));
    }

    [Fact]
    public void InvalidMinimumAndMaximumAreRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new Bounds3d(
                new Vector3d(10.0, 0.0, 0.0),
                new Vector3d(5.0, 10.0, 10.0)));
    }
}
