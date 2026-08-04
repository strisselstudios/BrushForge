using BrushForge.Geometry.Vectors;
using BrushForge.MapFormat.Model;

namespace BrushForge.Tests.MapFormat;

public sealed class MapModelTests
{
    [Fact]
    public void PropertyTrimsKeyButPreservesValue()
    {
        MapProperty property = new(
            "  message  ",
            "  preserved value  ");

        Assert.Equal(
            "message",
            property.Key);

        Assert.Equal(
            "  preserved value  ",
            property.Value);
    }

    [Fact]
    public void PropertyRejectsEmptyKey()
    {
        Assert.Throws<ArgumentException>(
            () => new MapProperty(
                "   ",
                "value"));
    }

    [Fact]
    public void PropertyRejectsLineBreaks()
    {
        Assert.Throws<ArgumentException>(
            () => new MapProperty(
                "message",
                "first`nsecond"));
    }

    [Fact]
    public void EntityFindsClassnameCaseInsensitively()
    {
        MapEntity entity = new(
        [
            new MapProperty(
                "ClassName",
                "light")
        ]);

        Assert.Equal(
            "light",
            entity.ClassName);
    }

    [Fact]
    public void DuplicatePropertyValuesRemainOrdered()
    {
        MapEntity entity = new(
        [
            new MapProperty(
                "target",
                "first"),
            new MapProperty(
                "target",
                "second")
        ]);

        Assert.Equal(
            ["first", "second"],
            entity.GetPropertyValues(
                "target"));
    }

    [Fact]
    public void WorldspawnFactoryPlacesClassnameFirst()
    {
        MapEntity entity =
            MapEntity.CreateWorldspawn(
                additionalProperties:
                [
                    new MapProperty(
                        "message",
                        "BrushForge")
                ]);

        Assert.True(entity.IsWorldspawn);

        Assert.Equal(
            "classname",
            entity.Properties[0].Key);

        Assert.Equal(
            "message",
            entity.Properties[1].Key);
    }

    [Fact]
    public void DocumentReturnsFirstWorldspawn()
    {
        MapEntity light = new(
        [
            new MapProperty(
                "classname",
                "light")
        ]);

        MapEntity worldspawn =
            MapEntity.CreateWorldspawn(
            [
                TestBrushFactory.CreateBox(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        64.0,
                        64.0))
            ]);

        MapDocument document = new(
        [
            light,
            worldspawn
        ]);

        Assert.Same(
            worldspawn,
            document.Worldspawn);
    }
}
