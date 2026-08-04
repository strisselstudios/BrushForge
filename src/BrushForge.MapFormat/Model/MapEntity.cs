using System.Collections.ObjectModel;
using BrushForge.Geometry.Brushes;

namespace BrushForge.MapFormat.Model;

/// <summary>
/// One ordered Valve map entity containing properties followed by brushes.
/// Duplicate property keys are preserved because imported formats may rely on
/// repeated keys.
/// </summary>
public sealed class MapEntity
{
    private readonly ReadOnlyCollection<MapProperty> _properties;
    private readonly ReadOnlyCollection<ConvexBrush> _brushes;

    public MapEntity(
        IEnumerable<MapProperty>? properties = null,
        IEnumerable<ConvexBrush>? brushes = null)
    {
        MapProperty[] propertyArray =
            properties?.ToArray() ?? [];

        ConvexBrush[] brushArray =
            brushes?.ToArray() ?? [];

        if (
            propertyArray.Any(
                property =>
                    property is null)
        ) {
            throw new ArgumentException(
                "An entity cannot contain null properties.",
                nameof(properties));
        }

        if (
            brushArray.Any(
                brush =>
                    brush is null)
        ) {
            throw new ArgumentException(
                "An entity cannot contain null brushes.",
                nameof(brushes));
        }

        _properties = Array.AsReadOnly(propertyArray);
        _brushes = Array.AsReadOnly(brushArray);
    }

    public IReadOnlyList<MapProperty> Properties => _properties;

    public IReadOnlyList<ConvexBrush> Brushes => _brushes;

    public string? ClassName =>
        _properties
            .FirstOrDefault(
                property =>
                    string.Equals(
                        property.Key,
                        "classname",
                        StringComparison.OrdinalIgnoreCase))
            ?.Value;

    public bool IsWorldspawn =>
        string.Equals(
            ClassName,
            "worldspawn",
            StringComparison.OrdinalIgnoreCase);

    public bool TryGetProperty(
        string key,
        out string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        MapProperty? property =
            _properties.FirstOrDefault(
                candidate =>
                    string.Equals(
                        candidate.Key,
                        key,
                        StringComparison.OrdinalIgnoreCase));

        if (property is null) {
            value = null;
            return false;
        }

        value = property.Value;
        return true;
    }

    public IReadOnlyList<string> GetPropertyValues(
        string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _properties
            .Where(
                property =>
                    string.Equals(
                        property.Key,
                        key,
                        StringComparison.OrdinalIgnoreCase))
            .Select(
                property =>
                    property.Value)
            .ToArray();
    }

    public static MapEntity CreateWorldspawn(
        IEnumerable<ConvexBrush>? brushes = null,
        IEnumerable<MapProperty>? additionalProperties = null)
    {
        MapProperty[] additional =
            additionalProperties?.ToArray() ?? [];

        if (
            additional.Any(
                property =>
                    string.Equals(
                        property.Key,
                        "classname",
                        StringComparison.OrdinalIgnoreCase))
        ) {
            throw new ArgumentException(
                "Additional worldspawn properties cannot redefine classname.",
                nameof(additionalProperties));
        }

        MapProperty[] properties =
        [
            new MapProperty(
                "classname",
                "worldspawn"),
            .. additional
        ];

        return new MapEntity(
            properties,
            brushes);
    }
}
