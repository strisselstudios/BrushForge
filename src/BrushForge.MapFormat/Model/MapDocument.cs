using System.Collections.ObjectModel;

namespace BrushForge.MapFormat.Model;

/// <summary>
/// Immutable ordered Valve map document.
/// </summary>
public sealed class MapDocument
{
    private readonly ReadOnlyCollection<MapEntity> _entities;

    public MapDocument(
        IEnumerable<MapEntity>? entities = null)
    {
        MapEntity[] entityArray =
            entities?.ToArray() ?? [];

        if (
            entityArray.Any(
                entity =>
                    entity is null)
        ) {
            throw new ArgumentException(
                "A map document cannot contain null entities.",
                nameof(entities));
        }

        _entities = Array.AsReadOnly(entityArray);
    }

    public IReadOnlyList<MapEntity> Entities => _entities;

    public MapEntity? Worldspawn =>
        _entities.FirstOrDefault(
            entity =>
                entity.IsWorldspawn);

    public static MapDocument CreateWorldspawnOnly(
        IEnumerable<Geometry.Brushes.ConvexBrush>? brushes = null,
        IEnumerable<MapProperty>? additionalProperties = null)
    {
        return new MapDocument(
        [
            MapEntity.CreateWorldspawn(
                brushes,
                additionalProperties)
        ]);
    }
}
