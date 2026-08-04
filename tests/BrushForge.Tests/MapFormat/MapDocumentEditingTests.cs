using BrushForge.Geometry.Vectors;
using BrushForge.MapFormat.Editing;
using BrushForge.MapFormat.Model;
using BrushForge.MapFormat.Parsing;
using BrushForge.MapFormat.Serialization;
using BrushForge.Tests.Geometry;

namespace BrushForge.Tests.MapFormat;

public sealed class MapDocumentEditingTests
{
    [Fact]
    public void AddEntityAppendsWithoutMutatingSource()
    {
        MapDocument source =
            MapDocument.CreateWorldspawnOnly();

        MapEntity light =
            CreatePointEntity(
                "light",
                "0 0 64");

        MapDocument edited =
            source.AddEntity(light);

        Assert.Single(source.Entities);
        Assert.Equal(2, edited.Entities.Count);
        Assert.Same(light, edited.Entities[1]);
    }

    [Fact]
    public void InsertEntityUsesRequestedIndex()
    {
        MapEntity light =
            CreatePointEntity(
                "light",
                "0 0 64");

        MapDocument source =
            MapDocument.CreateWorldspawnOnly();

        MapDocument edited =
            source.InsertEntity(
                0,
                light);

        Assert.Same(light, edited.Entities[0]);
        Assert.True(
            edited.Entities[1].IsWorldspawn);
    }

    [Fact]
    public void ReplaceEntityAtReplacesOnlySelectedEntity()
    {
        MapEntity light =
            CreatePointEntity(
                "light",
                "0 0 64");

        MapEntity player =
            CreatePointEntity(
                "info_player_start",
                "32 32 16");

        MapDocument source = new(
        [
            MapEntity.CreateWorldspawn(),
            light
        ]);

        MapDocument edited =
            source.ReplaceEntityAt(
                1,
                player);

        Assert.Same(
            source.Entities[0],
            edited.Entities[0]);

        Assert.Same(
            player,
            edited.Entities[1]);
    }

    [Fact]
    public void RemoveEntityAtRemovesOnlySelectedEntity()
    {
        MapEntity light =
            CreatePointEntity(
                "light",
                "0 0 64");

        MapDocument source = new(
        [
            MapEntity.CreateWorldspawn(),
            light
        ]);

        MapDocument edited =
            source.RemoveEntityAt(1);

        Assert.Single(edited.Entities);
        Assert.True(
            edited.Entities[0].IsWorldspawn);
    }

    [Fact]
    public void SetWorldspawnInsertsFirstWhenMissing()
    {
        MapEntity light =
            CreatePointEntity(
                "light",
                "0 0 64");

        MapEntity worldspawn =
            MapEntity.CreateWorldspawn();

        MapDocument source = new(
        [
            light
        ]);

        MapDocument edited =
            source.SetWorldspawn(
                worldspawn);

        Assert.Equal(2, edited.Entities.Count);
        Assert.Same(worldspawn, edited.Entities[0]);
        Assert.Same(light, edited.Entities[1]);
    }

    [Fact]
    public void SetWorldspawnReplacesAllExistingWorldspawns()
    {
        MapEntity firstWorldspawn =
            MapEntity.CreateWorldspawn(
                additionalProperties:
                [
                    new MapProperty(
                        "message",
                        "first")
                ]);

        MapEntity secondWorldspawn =
            MapEntity.CreateWorldspawn(
                additionalProperties:
                [
                    new MapProperty(
                        "message",
                        "second")
                ]);

        MapEntity light =
            CreatePointEntity(
                "light",
                "0 0 64");

        MapEntity replacement =
            MapEntity.CreateWorldspawn(
                additionalProperties:
                [
                    new MapProperty(
                        "message",
                        "replacement")
                ]);

        MapDocument source = new(
        [
            firstWorldspawn,
            light,
            secondWorldspawn
        ]);

        MapDocument edited =
            source.SetWorldspawn(
                replacement);

        Assert.Equal(2, edited.Entities.Count);
        Assert.Same(replacement, edited.Entities[0]);
        Assert.Same(light, edited.Entities[1]);

        Assert.Single(
            edited.Entities,
            entity =>
                entity.IsWorldspawn);
    }

    [Fact]
    public void SetWorldspawnPreservesNonWorldspawnOrder()
    {
        MapEntity light =
            CreatePointEntity(
                "light",
                "0 0 64");

        MapEntity player =
            CreatePointEntity(
                "info_player_start",
                "32 32 16");

        MapDocument source = new(
        [
            light,
            MapEntity.CreateWorldspawn(),
            player
        ]);

        MapDocument edited =
            source.SetWorldspawn(
                MapEntity.CreateWorldspawn());

        Assert.Equal(
            ["worldspawn", "light", "info_player_start"],
            edited.Entities.Select(
                entity =>
                    entity.ClassName));
    }

    [Fact]
    public void SetWorldspawnRejectsNonWorldspawnEntity()
    {
        MapDocument source =
            MapDocument.CreateWorldspawnOnly();

        MapEntity light =
            CreatePointEntity(
                "light",
                "0 0 64");

        Assert.Throws<ArgumentException>(
            () =>
                source.SetWorldspawn(
                    light));
    }

    [Fact]
    public void EditWorldspawnAppliesImmutableEdit()
    {
        MapDocument source =
            MapDocument.CreateWorldspawnOnly();

        MapDocument edited =
            source.EditWorldspawn(
                worldspawn =>
                    worldspawn.SetSingleProperty(
                        "message",
                        "Edited"));

        Assert.False(
            source.Worldspawn!.TryGetProperty(
                "message",
                out _));

        Assert.True(
            edited.Worldspawn!.TryGetProperty(
                "message",
                out string? message));

        Assert.Equal(
            "Edited",
            message);
    }

    [Fact]
    public void EditWorldspawnRejectsMissingOrInvalidResult()
    {
        MapDocument withoutWorldspawn = new(
        [
            CreatePointEntity(
                "light",
                "0 0 64")
        ]);

        Assert.Throws<InvalidOperationException>(
            () =>
                withoutWorldspawn.EditWorldspawn(
                    entity =>
                        entity));

        MapDocument source =
            MapDocument.CreateWorldspawnOnly();

        Assert.Throws<InvalidOperationException>(
            () =>
                source.EditWorldspawn(
                    _ =>
                        CreatePointEntity(
                            "light",
                            "0 0 64")));

        Assert.Throws<InvalidOperationException>(
            () =>
                source.EditWorldspawn(
                    _ =>
                        null!));
    }

    [Fact]
    public void EntityIndexesAreValidated()
    {
        MapDocument source =
            MapDocument.CreateWorldspawnOnly();

        MapEntity light =
            CreatePointEntity(
                "light",
                "0 0 64");

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.InsertEntity(
                    -1,
                    light));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.InsertEntity(
                    2,
                    light));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.ReplaceEntityAt(
                    1,
                    light));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                source.RemoveEntityAt(
                    1));
    }

    [Fact]
    public void EditedDocumentRoundTripsThroughValve220()
    {
        MapDocument source =
            MapDocument.CreateWorldspawnOnly(
            [
                TestBrushFactory.CreateBox(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        64.0,
                        64.0))
            ]);

        MapDocument edited =
            source
                .EditWorldspawn(
                    worldspawn =>
                        worldspawn.SetSingleProperty(
                            "message",
                            "Edited"))
                .AddEntity(
                    CreatePointEntity(
                        "light",
                        "32 32 96"));

        string first =
            Valve220MapWriter.Serialize(
                edited);

        MapDocument parsed =
            Valve220MapReader.Parse(
                first);

        string second =
            Valve220MapWriter.Serialize(
                parsed);

        Assert.Equal(
            first,
            second);
    }

    private static MapEntity CreatePointEntity(
        string className,
        string origin)
    {
        return new MapEntity(
        [
            new MapProperty(
                "classname",
                className),
            new MapProperty(
                "origin",
                origin)
        ]);
    }
}
