using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

public sealed class BrushFaceTextureNameTests
{
    [Fact]
    public void LeadingBraceTextureNameIsAccepted()
    {
        BrushFace face =
            TestBrushFactory.CreateFace(
                Vector3d.Zero,
                Vector3d.UnitX,
                Vector3d.UnitY,
                "{GLASS");

        Assert.Equal(
            "{GLASS",
            face.TextureName);
    }

    [Fact]
    public void NonLeadingOpeningBraceIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                TestBrushFactory.CreateFace(
                    Vector3d.Zero,
                    Vector3d.UnitX,
                    Vector3d.UnitY,
                    "GLA{SS"));
    }

    [Fact]
    public void ClosingBraceIsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                TestBrushFactory.CreateFace(
                    Vector3d.Zero,
                    Vector3d.UnitX,
                    Vector3d.UnitY,
                    "GLASS}"));
    }
}
