using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

public sealed class Vector3dTests
{
    [Fact]
    public void CrossProductConfirmsRightHandedCoordinates()
    {
        Vector3d result = Vector3d.Cross(
            Vector3d.UnitX,
            Vector3d.UnitY);

        Assert.Equal(
            Vector3d.UnitZ,
            result);
    }

    [Fact]
    public void NormalizeProducesUnitLength()
    {
        Vector3d vector = new(3.0, 4.0, 0.0);
        Vector3d normalized = vector.Normalize();

        Assert.True(
            normalized.NearlyEquals(
                new Vector3d(0.6, 0.8, 0.0)));

        Assert.True(
            Math.Abs(normalized.Length - 1.0) <= 0.000000001);
    }

    [Fact]
    public void NormalizeRejectsZeroVector()
    {
        Assert.Throws<InvalidOperationException>(
            () => Vector3d.Zero.Normalize());
    }

    [Fact]
    public void VectorArithmeticPreservesDoublePrecisionValues()
    {
        Vector3d first = new(10.5, -2.25, 8.0);
        Vector3d second = new(-0.5, 4.25, 2.0);

        Assert.Equal(
            new Vector3d(10.0, 2.0, 10.0),
            first + second);

        Assert.Equal(
            new Vector3d(11.0, -6.5, 6.0),
            first - second);

        Assert.Equal(
            new Vector3d(21.0, -4.5, 16.0),
            first * 2.0);
    }

    [Fact]
    public void DistanceUsesThreeDimensionalCoordinates()
    {
        Vector3d first = new(0.0, 0.0, 0.0);
        Vector3d second = new(2.0, 3.0, 6.0);

        Assert.Equal(
            7.0,
            first.DistanceTo(second));
    }
}
