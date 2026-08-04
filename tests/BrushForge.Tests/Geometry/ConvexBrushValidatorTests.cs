using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

public sealed class ConvexBrushValidatorTests
{
    [Fact]
    public void CubeReconstructsAsClosedValidBrush()
    {
        ConvexBrush brush =
            TestBrushFactory.CreateBox(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    64.0,
                    64.0));

        BrushValidationResult result =
            ConvexBrushValidator.Validate(brush);

        Assert.True(result.IsValid);
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Geometry);
        Assert.True(result.Geometry.IsClosed);
        Assert.Equal(8, result.Geometry.Vertices.Count);
        Assert.Equal(6, result.Geometry.Faces.Count);
        Assert.Equal(12, result.Geometry.EdgeCount);
    }

    [Fact]
    public void CubeBoundsAreCalculatedFromReconstructedVertices()
    {
        ConvexBrush brush =
            TestBrushFactory.CreateBox(
                new Vector3d(
                    -32.0,
                    -16.0,
                    8.0),
                new Vector3d(
                    32.0,
                    48.0,
                    72.0));

        BrushValidationResult result =
            ConvexBrushValidator.Validate(brush);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Geometry);

        Assert.True(
            result.Geometry.Bounds.Minimum.NearlyEquals(
                new Vector3d(
                    -32.0,
                    -16.0,
                    8.0)));

        Assert.True(
            result.Geometry.Bounds.Maximum.NearlyEquals(
                new Vector3d(
                    32.0,
                    48.0,
                    72.0)));
    }

    [Fact]
    public void CubeVolumeIsCalculatedCorrectly()
    {
        ConvexBrush brush =
            TestBrushFactory.CreateBox(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    64.0,
                    64.0));

        BrushValidationResult result =
            ConvexBrushValidator.Validate(brush);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Geometry);

        Assert.Equal(
            262144.0,
            result.Geometry.Volume,
            precision: 6);
    }

    [Fact]
    public void TetrahedronReconstructsAsValidBrush()
    {
        ConvexBrush brush =
            TestBrushFactory.CreateTetrahedron(
                size: 64.0);

        BrushValidationResult result =
            ConvexBrushValidator.Validate(brush);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Geometry);
        Assert.Equal(4, result.Geometry.Vertices.Count);
        Assert.Equal(4, result.Geometry.Faces.Count);
        Assert.Equal(6, result.Geometry.EdgeCount);

        Assert.Equal(
            (64.0 * 64.0 * 64.0) / 6.0,
            result.Geometry.Volume,
            precision: 6);
    }

    [Fact]
    public void ReconstructedFaceWindingMatchesOutwardNormal()
    {
        ConvexBrush brush =
            TestBrushFactory.CreateBox(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    64.0,
                    64.0));

        BrushValidationResult result =
            ConvexBrushValidator.Validate(brush);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Geometry);

        foreach (
            BrushFaceGeometry faceGeometry
            in result.Geometry.Faces
        ) {
            Vector3d first =
                result.Geometry.Vertices[
                    faceGeometry.VertexIndices[0]];

            Vector3d second =
                result.Geometry.Vertices[
                    faceGeometry.VertexIndices[1]];

            Vector3d third =
                result.Geometry.Vertices[
                    faceGeometry.VertexIndices[2]];

            Vector3d polygonNormal =
                Vector3d.Cross(
                    second - first,
                    third - first);

            Assert.True(
                Vector3d.Dot(
                    polygonNormal,
                    faceGeometry.Face.Plane.Normal) >
                0.0);
        }
    }

    [Fact]
    public void MissingBoxFaceIsRejectedAsOpen()
    {
        ConvexBrush complete =
            TestBrushFactory.CreateBox(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    64.0,
                    64.0));

        ConvexBrush openBrush = new(
            complete.Faces
                .Where(
                    (_, index) =>
                        index != 1));

        BrushValidationResult result =
            ConvexBrushValidator.Validate(openBrush);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrors);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code is
                    BrushValidationDiagnosticCodes.FaceHasTooFewVertices or
                    BrushValidationDiagnosticCodes.OpenOrNonManifoldEdges);
    }

    [Fact]
    public void ReversedFaceIsRejected()
    {
        ConvexBrush complete =
            TestBrushFactory.CreateBox(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    64.0,
                    64.0));

        BrushFace reversedTop =
            TestBrushFactory.CreateFace(
                new Vector3d(0.0, 0.0, 64.0),
                new Vector3d(0.0, 64.0, 64.0),
                new Vector3d(64.0, 0.0, 64.0));

        BrushFace[] faces =
            complete.Faces.ToArray();

        faces[1] = reversedTop;

        ConvexBrush brush = new(faces);

        BrushValidationResult result =
            ConvexBrushValidator.Validate(brush);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void RedundantPlaneIsRejected()
    {
        ConvexBrush complete =
            TestBrushFactory.CreateBox(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    64.0,
                    64.0));

        BrushFace redundantFace =
            TestBrushFactory.CreateFace(
                new Vector3d(128.0, 0.0, 0.0),
                new Vector3d(128.0, 64.0, 0.0),
                new Vector3d(128.0, 0.0, 64.0));

        ConvexBrush brush = new(
            complete.Faces.Append(
                redundantFace));

        BrushValidationResult result =
            ConvexBrushValidator.Validate(brush);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code ==
                BrushValidationDiagnosticCodes.FaceHasTooFewVertices);
    }

    [Fact]
    public void NearlyFlatBrushIsRejected()
    {
        ConvexBrush brush =
            TestBrushFactory.CreateBox(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    64.0,
                    0.00000001));

        BrushValidationResult result =
            ConvexBrushValidator.Validate(brush);

        Assert.False(result.IsValid);
        Assert.True(result.HasErrors);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code is
                    BrushValidationDiagnosticCodes.ExtentTooSmall or
                    BrushValidationDiagnosticCodes.VolumeTooSmall or
                    BrushValidationDiagnosticCodes.FaceHasTooFewVertices);
    }

    [Fact]
    public void ValidationSettingsRejectNonPositiveMinimumVolume()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BrushValidationSettings(
                minimumVolume: 0.0));
    }
}
