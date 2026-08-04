using System.Collections.ObjectModel;
using BrushForge.Core.Numerics;
using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Validation;

/// <summary>
/// Reconstructed finite geometry derived from a structural ConvexBrush.
/// </summary>
public sealed class ConvexBrushGeometry
{
    private readonly ReadOnlyCollection<Vector3d> _vertices;
    private readonly ReadOnlyCollection<BrushFaceGeometry> _faces;

    public ConvexBrushGeometry(
        ConvexBrush sourceBrush,
        IEnumerable<Vector3d> vertices,
        IEnumerable<BrushFaceGeometry> faces,
        Bounds3d bounds,
        Vector3d centroid,
        double volume,
        int edgeCount,
        bool isClosed)
    {
        ArgumentNullException.ThrowIfNull(sourceBrush);
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(faces);

        Vector3d[] vertexArray = vertices.ToArray();
        BrushFaceGeometry[] faceArray = faces.ToArray();

        if (vertexArray.Length == 0) {
            throw new ArgumentException(
                "Reconstructed brush geometry requires at least one vertex.",
                nameof(vertices));
        }

        if (vertexArray.Any(vertex => !vertex.IsFinite)) {
            throw new ArgumentException(
                "Reconstructed brush vertices must be finite.",
                nameof(vertices));
        }

        if (!centroid.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(centroid),
                centroid,
                "The reconstructed centroid must be finite.");
        }

        if (
            !NumericTolerances.IsFinite(volume) ||
            volume < 0.0
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(volume),
                volume,
                "Brush volume must be finite and non-negative.");
        }

        if (edgeCount < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(edgeCount),
                edgeCount,
                "The edge count cannot be negative.");
        }

        SourceBrush = sourceBrush;
        _vertices = Array.AsReadOnly(vertexArray);
        _faces = Array.AsReadOnly(faceArray);
        Bounds = bounds;
        Centroid = centroid;
        Volume = volume;
        EdgeCount = edgeCount;
        IsClosed = isClosed;
    }

    public ConvexBrush SourceBrush { get; }

    public IReadOnlyList<Vector3d> Vertices => _vertices;

    public IReadOnlyList<BrushFaceGeometry> Faces => _faces;

    public Bounds3d Bounds { get; }

    public Vector3d Centroid { get; }

    public double Volume { get; }

    public int EdgeCount { get; }

    public bool IsClosed { get; }
}
