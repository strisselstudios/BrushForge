using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.MapFormat.Editing;
using BrushForge.MapFormat.Model;
using BrushForge.Tests.Geometry;

namespace BrushForge.Tests.MapFormat;

public sealed class MapEntityEditingTests
{
    [Fact]
    public void AddPropertyAppendsWithoutMutatingSource()
    {
        MapEntity source =
            MapEntity.CreateWorldspawn();

        MapProperty property =
            new(
                "message",
                "BrushForge");

        MapEntity edited =
            source.AddProperty(property);

        Assert.Single(source.Properties);
        Assert.Equal(2, edited.Properties.Count);
        Assert.Same(property, edited.Properties[1]);
    }

    [Fact]
    public void InsertPropertyUsesRequestedIndex()
    {
        MapEntity source =
            MapEntity.CreateWorldspawn(
                additionalProperties:
                [
                    new MapProperty(
                        "wad",
                        "textures.wad")
                ]);

        MapEntity edited =
            source.InsertProperty(
                1,
                new MapProperty(
                    "message",
                    "Inserted"));

        Assert.Equal(
            ["classname", "message", "wad"],
            edited.Properties.Select(
                property =>
                    property.Key));
    }

    [Fact]
    public void ReplacePropertyAtReplacesOnlySelectedEntry()
    {
        MapEntity source =
            MapEntity.CreateWorldspawn(
                additionalProperties:
                [
                    new MapProperty(
                        "message",
                        "Before"),
                    new MapProperty(
                        "wad",
                        "textures.wad")
                ]);

        MapProperty replacement =
            new(
                "message",
                "After");

        MapEntity edited =
            source.ReplacePropertyAt(
                1,
                replacement);

        Assert.Same(
            source.Properties[0],
            edited.Properties[0]);

        Assert.Same(
            replacement,
            edited.Properties[1]);

        Assert.Same(
            source.Properties[2],
            edited.Properties[2]);
    }

    [Fact]
    public void RemovePropertyAtRemovesOnlySelectedEntry()
    {
        MapEntity source =
            MapEntity.CreateWorldspawn(
                additionalProperties:
                [
                    new MapProperty(
                        "message",
                        "Remove"),
                    new MapProperty(
                        "wad",
                        "textures.wad")
                ]);

        MapEntity edited =
            source.RemovePropertyAt(1);

        Assert.Equal(
            ["classname", "wad"],
            edited.Properties.Select(
                property =>
                    property.Key));
    }

    [Fact]
    public void SetSinglePropertyAppendsMissingKey()
    {
        MapEntity source =
            MapEntity.CreateWorldspawn();

        MapEntity edited =
            source.SetSingleProperty(
                "message",
                "Added");

        Assert.Equal(
            "Added",
            edited.GetPropertyValues(
                "message")
                .Single());
    }

    [Fact]
    public void SetSinglePropertyReplacesFirstAndRemovesDuplicates()
    {
        MapEntity source = new(
        [
            new MapProperty(
                "classname",
                "light"),
            new MapProperty(
                "target",
                "first"),
            new MapProperty(
                "origin",
                "0 0 64"),
            new MapProperty(
                "target",
                "second")
        ]);

        MapEntity edited =
            source.SetSingleProperty(
                "target",
                "replacement");

        Assert.Equal(
            ["classname", "target", "origin"],
            edited.Properties.Select(
                property =>
                    property.Key));

        Assert.Equal(
            ["replacement"],
            edited.GetPropertyValues(
                "target"));
    }

    [Fact]
    public void SetSinglePropertyMatchesKeysCaseInsensitively()
    {
        MapEntity source = new(
        [
            new MapProperty(
                "ClassName",
                "light")
        ]);

        MapEntity edited =
            source.SetSingleProperty(
                "classname",
                "info_player_start");

        Assert.Single(edited.Properties);

        Assert.Equal(
            "classname",
            edited.Properties[0].Key);

        Assert.Equal(
            "info_player_start",
            edited.ClassName);
    }

    [Fact]
    public void RemovePropertiesRemovesEveryMatchingKey()
    {
        MapEntity source = new(
        [
            new MapProperty(
                "classname",
                "light"),
            new MapProperty(
                "target",
                "first"),
            new MapProperty(
                "TARGET",
                "second")
        ]);

        MapEntity edited =
            source.RemoveProperties(
                "target");

        Assert.Single(edited.Properties);
        Assert.Equal(
            "classname",
            edited.Properties[0].Key);
    }

    [Fact]
    public void WithClassNameReplacesDuplicateClassnames()
    {
        MapEntity source = new(
        [
            new MapProperty(
                "classname",
                "light"),
            new MapProperty(
                "ClassName",
                "duplicate"),
            new MapProperty(
                "origin",
                "0 0 64")
        ]);

        MapEntity edited =
            source.WithClassName(
                "info_player_start");

        Assert.Equal(
            ["info_player_start"],
            edited.GetPropertyValues(
                "classname"));

        Assert.Equal(
            2,
            edited.Properties.Count);
    }

    [Fact]
    public void AddBrushAppendsWithoutMutatingSource()
    {
        ConvexBrush first =
            CreateBrush(0.0);

        ConvexBrush second =
            CreateBrush(128.0);

        MapEntity source =
            MapEntity.CreateWorldspawn(
            [
                first
            ]);

        MapEntity edited =
            source.AddBrush(second);

        Assert.Single(source.Brushes);
        Assert.Equal(2, edited.Brushes.Count);
        Assert.Same(second, edited.Brushes[1]);
    }

    [Fact]
    public void InsertBrushUsesRequestedIndex()
    {
        ConvexBrush first =
            CreateBrush(0.0);

        ConvexBrush second =
            CreateBrush(128.0);

        ConvexBrush inserted =
            CreateBrush(256.0);

        MapEntity source =
            MapEntity.CreateWorldspawn(
            [
                first,
                second
            ]);

        MapEntity edited =
            source.InsertBrush(
                1,
                inserted);

        Assert.Same(first, edited.Brushes[0]);
        Assert.Same(inserted, edited.Brushes[1]);
        Assert.Same(second, edited.Brushes[2]);
    }

    [Fact]
    public void ReplaceBrushAtReplacesOnlySelectedBrush()
    {
        ConvexBrush first =
            CreateBrush(0.0);

        ConvexBrush second =
            CreateBrush(128.0);

        ConvexBrush replacement =
            CreateBrush(256.0);

        MapEntity source =
            MapEntity.CreateWorldspawn(
            [
                first,
                second
            ]);

        MapEntity edited =
            source.ReplaceBrushAt(
                1,
                replacement);

        Assert.Same(first, edited.Brushes[0]);
        Assert.Same(replacement, edited.Brushes[1]);
    }

    [Fact]
    public void RemoveBrushAtRemovesOnlySelectedBrush()
    {
        ConvexBrush first =
            CreateBrush(0.0);

        ConvexBrush second =
            CreateBrush(128.0);

        MapEntity source =
            MapEntity.CreateWorldspawn(
            [
                first,
                second
            ]);

        MapEntity edited =
            source.RemoveBrushAt(0);

        Assert.Single(edited.Brushes);
        Assert.Same(second, edited.Brushes[0]);
    }

    [Fact]
    public void PropertyIndexesAreValidated()
    {
        MapEntity source =
            MapEntity.CreateWorldspawn();

        MapProperty property =
            new(
                "message",
                "value");

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.InsertProperty(
                    -1,
                    property));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.InsertProperty(
                    2,
                    property));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.ReplacePropertyAt(
                    1,
                    property));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.RemovePropertyAt(
                    1));
    }

    [Fact]
    public void BrushIndexesAreValidated()
    {
        MapEntity source =
            MapEntity.CreateWorldspawn();

        ConvexBrush brush =
            CreateBrush(0.0);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.InsertBrush(
                    -1,
                    brush));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.InsertBrush(
                    1,
                    brush));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.ReplaceBrushAt(
                    0,
                    brush));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.RemoveBrushAt(
                    0));
    }

    private static ConvexBrush CreateBrush(
        double xOffset)
    {
        return TestBrushFactory.CreateBox(
            new Vector3d(
                xOffset,
                0.0,
                0.0),
            new Vector3d(
                xOffset + 64.0,
                64.0,
                64.0));
    }
}
