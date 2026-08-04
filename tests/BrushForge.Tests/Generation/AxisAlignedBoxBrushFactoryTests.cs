using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Geometry;

namespace BrushForge.Tests.Generation;

public sealed class AxisAlignedBoxBrushFactoryTests
{
    [Fact]
    public void CreateProducesValidSixFaceBrush()
    {
        Bounds3d bounds = new(
            new Vector3d(
                -16.0,
                -24.0,
                8.0),
            new Vector3d(
                32.0,
                40.0,
                96.0));

        ConvexBrush brush =
            AxisAlignedBoxBrushFactory.Create(
                bounds,
                "STONE");

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(
                brush);

        Assert.Equal(6, brush.FaceCount);
        Assert.True(validation.IsValid);
        Assert.Equal(bounds, validation.Geometry!.Bounds);
    }

    [Fact]
    public void CreateAppliesTextureToEveryFace()
    {
        ConvexBrush brush =
            AxisAlignedBoxBrushFactory.Create(
                new Bounds3d(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        64.0,
                        64.0)),
                "BARK");

        Assert.All(
            brush.Faces,
            face =>
                Assert.Equal(
                    "BARK",
                    face.TextureName));
    }

    [Fact]
    public void CreateRejectsDegenerateBounds()
    {
        Bounds3d bounds = new(
            Vector3d.Zero,
            new Vector3d(
                0.0,
                64.0,
                64.0));

        Assert.Throws<ArgumentException>(
            () =>
                AxisAlignedBoxBrushFactory.Create(
                    bounds,
                    "STONE"));
    }
}
