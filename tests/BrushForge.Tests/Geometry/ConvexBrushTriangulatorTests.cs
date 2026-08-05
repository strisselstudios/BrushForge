using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Triangulation;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

public sealed class ConvexBrushTriangulatorTests
{
    [Fact]
    public void BoxTriangulatesToTwelveTriangles()
    {
        ConvexBrushGeometry geometry =
            Validate(
                TestBrushFactory.CreateBox(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        64.0,
                        64.0)));

        TriangulatedBrushMesh mesh =
            ConvexBrushTriangulator.Triangulate(
                geometry);

        Assert.Equal(8, mesh.Vertices.Count);
        Assert.Equal(12, mesh.TriangleCount);
        Assert.Equal(36, mesh.TriangleIndices.Count);
    }

    [Fact]
    public void TetrahedronTriangulatesToFourTriangles()
    {
        ConvexBrushGeometry geometry =
            Validate(
                TestBrushFactory.CreateTetrahedron());

        TriangulatedBrushMesh mesh =
            ConvexBrushTriangulator.Triangulate(
                geometry);

        Assert.Equal(4, mesh.Vertices.Count);
        Assert.Equal(4, mesh.TriangleCount);
        Assert.Equal(12, mesh.TriangleIndices.Count);
    }

    [Fact]
    public void TriangleWindingMatchesEveryOutwardFaceNormal()
    {
        ConvexBrushGeometry geometry =
            Validate(
                TestBrushFactory.CreateBox(
                    new Vector3d(
                        -32.0,
                        -16.0,
                        8.0),
                    new Vector3d(
                        32.0,
                        48.0,
                        72.0)));

        TriangulatedBrushMesh mesh =
            ConvexBrushTriangulator.Triangulate(
                geometry);

        int triangleIndex = 0;

        foreach (BrushFaceGeometry face in geometry.Faces) {
            for (
                int polygonIndex = 1;
                polygonIndex < face.VertexIndices.Count - 1;
                polygonIndex++
            ) {
                int firstIndex =
                    mesh.TriangleIndices[triangleIndex++];

                int secondIndex =
                    mesh.TriangleIndices[triangleIndex++];

                int thirdIndex =
                    mesh.TriangleIndices[triangleIndex++];

                Vector3d first =
                    mesh.Vertices[firstIndex];

                Vector3d second =
                    mesh.Vertices[secondIndex];

                Vector3d third =
                    mesh.Vertices[thirdIndex];

                Vector3d triangleNormal =
                    Vector3d.Cross(
                        second - first,
                        third - first);

                Assert.True(
                    Vector3d.Dot(
                        triangleNormal,
                        face.Face.Plane.Normal) >
                    0.0);
            }
        }

        Assert.Equal(
            mesh.TriangleIndices.Count,
            triangleIndex);
    }

    [Fact]
    public void OpenGeometryIsRejected()
    {
        ConvexBrushGeometry validGeometry =
            Validate(
                TestBrushFactory.CreateTetrahedron());

        ConvexBrushGeometry openGeometry = new(
            validGeometry.SourceBrush,
            validGeometry.Vertices,
            validGeometry.Faces,
            validGeometry.Bounds,
            validGeometry.Centroid,
            validGeometry.Volume,
            validGeometry.EdgeCount,
            isClosed: false);

        Assert.Throws<InvalidOperationException>(
            () =>
                ConvexBrushTriangulator.Triangulate(
                    openGeometry));
    }

    private static ConvexBrushGeometry Validate(
        ConvexBrush brush)
    {
        BrushValidationResult validation =
            ConvexBrushValidator.Validate(
                brush);

        Assert.True(validation.IsValid);
        Assert.NotNull(validation.Geometry);

        return validation.Geometry!;
    }
}
