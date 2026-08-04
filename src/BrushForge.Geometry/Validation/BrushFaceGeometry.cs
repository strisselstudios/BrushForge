using System.Collections.ObjectModel;
using BrushForge.Core.Numerics;
using BrushForge.Geometry.Brushes;

namespace BrushForge.Geometry.Validation;

/// <summary>
/// One reconstructed polygon belonging to a source brush face.
/// Vertex indices are ordered around the outward face normal.
/// </summary>
public sealed class BrushFaceGeometry
{
    private readonly ReadOnlyCollection<int> _vertexIndices;

    public BrushFaceGeometry(
        BrushFace face,
        IEnumerable<int> vertexIndices,
        double area)
    {
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(vertexIndices);

        int[] indices = vertexIndices.ToArray();

        if (indices.Length < 3) {
            throw new ArgumentException(
                "A reconstructed face polygon requires at least three vertices.",
                nameof(vertexIndices));
        }

        if (
            indices.Any(index => index < 0) ||
            indices.Distinct().Count() != indices.Length
        ) {
            throw new ArgumentException(
                "Face vertex indices must be non-negative and unique.",
                nameof(vertexIndices));
        }

        if (
            !NumericTolerances.IsFinite(area) ||
            area < 0.0
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(area),
                area,
                "Face area must be finite and non-negative.");
        }

        Face = face;
        _vertexIndices = Array.AsReadOnly(indices);
        Area = area;
    }

    public BrushFace Face { get; }

    public IReadOnlyList<int> VertexIndices => _vertexIndices;

    public double Area { get; }
}
