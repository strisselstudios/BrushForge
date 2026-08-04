using BrushForge.Core.Coordinates;
using BrushForge.Core.Grid;
using BrushForge.Core.Numerics;

namespace BrushForge.Tests.Core;

public sealed class CoordinateInfrastructureTests
{
    [Fact]
    public void CoordinateSystemUsesRightHandedZUpConvention()
    {
        Assert.Equal(
            CoordinateAxis.X,
            BrushForgeCoordinateSystem.RightAxis);

        Assert.Equal(
            CoordinateAxis.Y,
            BrushForgeCoordinateSystem.ForwardAxis);

        Assert.Equal(
            CoordinateAxis.Z,
            BrushForgeCoordinateSystem.UpAxis);

        Assert.Equal(
            CoordinateHandedness.RightHanded,
            BrushForgeCoordinateSystem.Handedness);

        Assert.Equal(
            1.0,
            BrushForgeCoordinateSystem.MapUnitsPerInternalUnit);
    }

    [Fact]
    public void CoordinateScaleConvertsInBothDirections()
    {
        CoordinateScale scale = new(32.0);

        Assert.Equal(64.0, scale.ToMapUnits(2.0));
        Assert.Equal(2.0, scale.FromMapUnits(64.0));
    }

    [Fact]
    public void CoordinateScaleRejectsInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CoordinateScale(0.0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CoordinateScale(double.NaN));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CoordinateScale.Identity.ToMapUnits(double.PositiveInfinity));
    }

    [Theory]
    [InlineData(12.0, 16.0)]
    [InlineData(-12.0, -16.0)]
    [InlineData(11.9, 8.0)]
    [InlineData(-11.9, -8.0)]
    [InlineData(0.0, 0.0)]
    public void GridSnappingUsesMidpointAwayFromZero(
        double input,
        double expected)
    {
        Assert.Equal(
            expected,
            GridSpacing.Eight.Snap(input));
    }

    [Fact]
    public void GridAlignmentUsesCentralTolerance()
    {
        Assert.True(
            GridSpacing.Eight.IsAligned(16.0));

        Assert.True(
            GridSpacing.Eight.IsAligned(
                16.0 + (NumericTolerances.GridAlignment / 2.0)));

        Assert.False(
            GridSpacing.Eight.IsAligned(16.25));
    }
}
