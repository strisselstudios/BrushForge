using System.Collections.ObjectModel;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Triangulation;

/// <summary>
/// Immutable triangle mesh reconstructed from one validated convex brush.
/// Triangle indices use the source geometry's outward-facing winding.
/// </summary>
public sealed class TriangulatedBrushMesh
{
    private readonly ReadOnlyCollection<Vector3d> _vertices;
    private readonly ReadOnlyCollection<int> _triangleIndices;

    public TriangulatedBrushMesh(
        IEnumerable<Vector3d> vertices,
        IEnumerable<int> triangleIndices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(triangleIndices);

        Vector3d[] vertexArray = vertices.ToArray();
        int[] indexArray = triangleIndices.ToArray();

        if (vertexArray.Length < 4) {
            throw new ArgumentException(
                "A closed brush mesh requires at least four vertices.",
                nameof(vertices));
        }

        if (vertexArray.Any(vertex => !vertex.IsFinite)) {
            throw new ArgumentException(
                "Brush mesh vertices must be finite.",
                nameof(vertices));
        }

        if (
            indexArray.Length == 0 ||
            indexArray.Length % 3 != 0
        ) {
            throw new ArgumentException(
                "Brush mesh triangle indices must contain complete triangles.",
                nameof(triangleIndices));
        }

        if (
            indexArray.Any(
                index =>
                    index < 0 ||
                    index >= vertexArray.Length)
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(triangleIndices),
                "Brush mesh triangle indices must reference existing vertices.");
        }

        _vertices = Array.AsReadOnly(vertexArray);
        _triangleIndices = Array.AsReadOnly(indexArray);
    }

    public IReadOnlyList<Vector3d> Vertices => _vertices;

    public IReadOnlyList<int> TriangleIndices => _triangleIndices;

    public int TriangleCount =>
        _triangleIndices.Count / 3;
}
