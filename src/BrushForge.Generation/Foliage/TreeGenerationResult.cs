using System.Collections.ObjectModel;
using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.MapFormat.Model;

namespace BrushForge.Generation.Foliage;

/// <summary>
/// Immutable output of one deterministic tree generation operation.
/// </summary>
public sealed class TreeGenerationResult
{
    private readonly ReadOnlyCollection<GeneratedTreeBrush> _parts;
    private readonly ReadOnlyCollection<ConvexBrush> _brushes;

    public TreeGenerationResult(
        IEnumerable<GeneratedTreeBrush> parts,
        MapDocument document)
    {
        ArgumentNullException.ThrowIfNull(parts);
        ArgumentNullException.ThrowIfNull(document);

        GeneratedTreeBrush[] partArray =
            parts.ToArray();

        if (partArray.Length == 0) {
            throw new ArgumentException(
                "A generated tree requires at least one brush part.",
                nameof(parts));
        }

        if (
            partArray.Any(
                part =>
                    part is null)
        ) {
            throw new ArgumentException(
                "Generated tree parts cannot contain null values.",
                nameof(parts));
        }

        ConvexBrush[] brushArray =
            partArray
                .Select(
                    part =>
                        part.Brush)
                .ToArray();

        Bounds3d bounds =
            partArray[0].Bounds;

        for (
            int index = 1;
            index < partArray.Length;
            index++
        ) {
            bounds = bounds.Union(
                partArray[index].Bounds);
        }

        _parts = Array.AsReadOnly(partArray);
        _brushes = Array.AsReadOnly(brushArray);
        Document = document;
        Bounds = bounds;
    }

    public IReadOnlyList<GeneratedTreeBrush> Parts =>
        _parts;

    public IReadOnlyList<ConvexBrush> Brushes =>
        _brushes;

    public int BrushCount =>
        _brushes.Count;

    public MapDocument Document { get; }

    public Bounds3d Bounds { get; }
}
