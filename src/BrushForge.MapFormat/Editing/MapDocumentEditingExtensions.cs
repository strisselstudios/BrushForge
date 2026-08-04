using BrushForge.MapFormat.Model;

namespace BrushForge.MapFormat.Editing;

/// <summary>
/// Immutable editing operations for ordered map entities and worldspawn.
/// </summary>
public static class MapDocumentEditingExtensions
{
    public static MapDocument WithEntities(
        this MapDocument document,
        IEnumerable<MapEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(entities);

        return new MapDocument(entities);
    }

    public static MapDocument AddEntity(
        this MapDocument document,
        MapEntity entity)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(entity);

        MapEntity[] entities =
        [
            .. document.Entities,
            entity
        ];

        return new MapDocument(entities);
    }

    public static MapDocument InsertEntity(
        this MapDocument document,
        int index,
        MapEntity entity)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(entity);

        ValidateInsertionIndex(
            index,
            document.Entities.Count,
            nameof(index));

        List<MapEntity> entities =
            new(document.Entities);

        entities.Insert(
            index,
            entity);

        return new MapDocument(entities);
    }

    public static MapDocument ReplaceEntityAt(
        this MapDocument document,
        int index,
        MapEntity entity)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(entity);

        ValidateExistingIndex(
            index,
            document.Entities.Count,
            nameof(index));

        MapEntity[] entities =
            document.Entities.ToArray();

        entities[index] = entity;

        return new MapDocument(entities);
    }

    public static MapDocument RemoveEntityAt(
        this MapDocument document,
        int index)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidateExistingIndex(
            index,
            document.Entities.Count,
            nameof(index));

        List<MapEntity> entities =
            new(document.Entities);

        entities.RemoveAt(index);

        return new MapDocument(entities);
    }

    /// <summary>
    /// Places the supplied worldspawn first and removes every previous
    /// worldspawn while preserving the relative order of non-worldspawn
    /// entities.
    /// </summary>
    public static MapDocument SetWorldspawn(
        this MapDocument document,
        MapEntity worldspawn)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(worldspawn);

        if (!worldspawn.IsWorldspawn) {
            throw new ArgumentException(
                "The replacement entity must have classname worldspawn.",
                nameof(worldspawn));
        }

        List<MapEntity> entities =
            new(document.Entities.Count + 1)
            {
                worldspawn
            };

        foreach (
            MapEntity entity in
            document.Entities
        ) {
            if (!entity.IsWorldspawn) {
                entities.Add(entity);
            }
        }

        return new MapDocument(entities);
    }

    public static MapDocument EditWorldspawn(
        this MapDocument document,
        Func<MapEntity, MapEntity> editor)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(editor);

        MapEntity? currentWorldspawn =
            document.Worldspawn;

        if (currentWorldspawn is null) {
            throw new InvalidOperationException(
                "The map document does not contain a worldspawn entity.");
        }

        MapEntity editedWorldspawn =
            editor(currentWorldspawn) ??
            throw new InvalidOperationException(
                "The worldspawn editor returned null.");

        if (!editedWorldspawn.IsWorldspawn) {
            throw new InvalidOperationException(
                "The worldspawn editor must return an entity whose classname is worldspawn.");
        }

        return document.SetWorldspawn(
            editedWorldspawn);
    }

    private static void ValidateExistingIndex(
        int index,
        int count,
        string parameterName)
    {
        if (
            index < 0 ||
            index >= count
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                index,
                $"The index must be between 0 and {count - 1}.");
        }
    }

    private static void ValidateInsertionIndex(
        int index,
        int count,
        string parameterName)
    {
        if (
            index < 0 ||
            index > count
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                index,
                $"The insertion index must be between 0 and {count}.");
        }
    }
}
