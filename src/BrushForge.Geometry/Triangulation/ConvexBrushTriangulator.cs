using BrushForge.Geometry.Validation;

namespace BrushForge.Geometry.Triangulation;

/// <summary>
/// Converts reconstructed convex brush polygons into triangle fans suitable
/// for preview rendering.
/// </summary>
public static class ConvexBrushTriangulator
{
    public static TriangulatedBrushMesh Triangulate(
        ConvexBrushGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (!geometry.IsClosed) {
            throw new InvalidOperationException(
                "Only closed convex brush geometry can be triangulated.");
        }

        List<int> triangleIndices = [];

        foreach (BrushFaceGeometry face in geometry.Faces) {
            int firstVertexIndex =
                face.VertexIndices[0];

            for (
                int index = 1;
                index < face.VertexIndices.Count - 1;
                index++
            ) {
                triangleIndices.Add(firstVertexIndex);
                triangleIndices.Add(
                    face.VertexIndices[index]);
                triangleIndices.Add(
                    face.VertexIndices[index + 1]);
            }
        }

        return new TriangulatedBrushMesh(
            geometry.Vertices,
            triangleIndices);
    }
}
