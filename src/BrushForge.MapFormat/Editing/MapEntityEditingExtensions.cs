using BrushForge.Geometry.Brushes;
using BrushForge.MapFormat.Model;

namespace BrushForge.MapFormat.Editing;

/// <summary>
/// Immutable editing operations for ordered entity properties and brushes.
/// </summary>
public static class MapEntityEditingExtensions
{
    public static MapEntity WithProperties(
        this MapEntity entity,
        IEnumerable<MapProperty> properties)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(properties);

        return new MapEntity(
            properties,
            entity.Brushes);
    }

    public static MapEntity AddProperty(
        this MapEntity entity,
        MapProperty property)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(property);

        MapProperty[] properties =
        [
            .. entity.Properties,
            property
        ];

        return new MapEntity(
            properties,
            entity.Brushes);
    }

    public static MapEntity InsertProperty(
        this MapEntity entity,
        int index,
        MapProperty property)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(property);

        ValidateInsertionIndex(
            index,
            entity.Properties.Count,
            nameof(index));

        List<MapProperty> properties =
            new(entity.Properties);

        properties.Insert(
            index,
            property);

        return new MapEntity(
            properties,
            entity.Brushes);
    }

    public static MapEntity ReplacePropertyAt(
        this MapEntity entity,
        int index,
        MapProperty property)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(property);

        ValidateExistingIndex(
            index,
            entity.Properties.Count,
            nameof(index));

        MapProperty[] properties =
            entity.Properties.ToArray();

        properties[index] = property;

        return new MapEntity(
            properties,
            entity.Brushes);
    }

    public static MapEntity RemovePropertyAt(
        this MapEntity entity,
        int index)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ValidateExistingIndex(
            index,
            entity.Properties.Count,
            nameof(index));

        List<MapProperty> properties =
            new(entity.Properties);

        properties.RemoveAt(index);

        return new MapEntity(
            properties,
            entity.Brushes);
    }

    /// <summary>
    /// Replaces the first case-insensitive key match in place, removes later
    /// duplicates, or appends the property when the key is not present.
    /// </summary>
    public static MapEntity SetSingleProperty(
        this MapEntity entity,
        string key,
        string value)
    {
        ArgumentNullException.ThrowIfNull(entity);

        MapProperty replacement =
            new(
                key,
                value);

        List<MapProperty> properties =
            new(entity.Properties.Count + 1);

        bool replacementAdded = false;

        foreach (
            MapProperty property in
            entity.Properties
        ) {
            bool matches =
                string.Equals(
                    property.Key,
                    replacement.Key,
                    StringComparison.OrdinalIgnoreCase);

            if (!matches) {
                properties.Add(property);
                continue;
            }

            if (!replacementAdded) {
                properties.Add(replacement);
                replacementAdded = true;
            }
        }

        if (!replacementAdded) {
            properties.Add(replacement);
        }

        return new MapEntity(
            properties,
            entity.Brushes);
    }

    public static MapEntity RemoveProperties(
        this MapEntity entity,
        string key)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        MapProperty[] properties =
            entity.Properties
                .Where(
                    property =>
                        !string.Equals(
                            property.Key,
                            key,
                            StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (
            properties.Length ==
            entity.Properties.Count
        ) {
            return entity;
        }

        return new MapEntity(
            properties,
            entity.Brushes);
    }

    public static MapEntity WithClassName(
        this MapEntity entity,
        string className)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity.SetSingleProperty(
            "classname",
            className);
    }

    public static MapEntity WithBrushes(
        this MapEntity entity,
        IEnumerable<ConvexBrush> brushes)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(brushes);

        return new MapEntity(
            entity.Properties,
            brushes);
    }

    public static MapEntity AddBrush(
        this MapEntity entity,
        ConvexBrush brush)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(brush);

        ConvexBrush[] brushes =
        [
            .. entity.Brushes,
            brush
        ];

        return new MapEntity(
            entity.Properties,
            brushes);
    }

    public static MapEntity InsertBrush(
        this MapEntity entity,
        int index,
        ConvexBrush brush)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(brush);

        ValidateInsertionIndex(
            index,
            entity.Brushes.Count,
            nameof(index));

        List<ConvexBrush> brushes =
            new(entity.Brushes);

        brushes.Insert(
            index,
            brush);

        return new MapEntity(
            entity.Properties,
            brushes);
    }

    public static MapEntity ReplaceBrushAt(
        this MapEntity entity,
        int index,
        ConvexBrush brush)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(brush);

        ValidateExistingIndex(
            index,
            entity.Brushes.Count,
            nameof(index));

        ConvexBrush[] brushes =
            entity.Brushes.ToArray();

        brushes[index] = brush;

        return new MapEntity(
            entity.Properties,
            brushes);
    }

    public static MapEntity RemoveBrushAt(
        this MapEntity entity,
        int index)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ValidateExistingIndex(
            index,
            entity.Brushes.Count,
            nameof(index));

        List<ConvexBrush> brushes =
            new(entity.Brushes);

        brushes.RemoveAt(index);

        return new MapEntity(
            entity.Properties,
            brushes);
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
