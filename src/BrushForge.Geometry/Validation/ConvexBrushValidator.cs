using BrushForge.Core.Diagnostics;
using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Intersections;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Validation;

/// <summary>
/// Reconstructs and validates the finite polyhedron described by outward
/// oriented convex brush planes.
///
/// Interior points must satisfy:
/// face.Plane.SignedDistanceTo(point) <= tolerance.
/// </summary>
public static class ConvexBrushValidator
{
    public static BrushValidationResult Validate(
        ConvexBrush brush,
        BrushValidationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(brush);

        settings ??= BrushValidationSettings.Default;

        DiagnosticBag diagnostics = new();

        List<Vector3d> vertices = ReconstructVertices(
            brush,
            settings);

        if (vertices.Count == 0) {
            diagnostics.Add(
                BrushValidationDiagnosticCodes.NoVertices,
                DiagnosticSeverity.Error,
                "The brush planes produced no finite enclosed vertices.");

            return new BrushValidationResult(
                geometry: null,
                diagnostics.Snapshot());
        }

        Vector3d centroid = CalculateCentroid(vertices);

        List<BrushFaceGeometry> faceGeometries =
            ReconstructFaces(
                brush,
                vertices,
                settings,
                diagnostics);

        ValidateCentroidAgainstPlanes(
            brush,
            centroid,
            settings,
            diagnostics);

        Bounds3d bounds = Bounds3d.FromPoints(vertices);

        ValidateBounds(
            bounds,
            settings,
            diagnostics);

        EdgeValidation edgeValidation =
            ValidateEdges(
                faceGeometries,
                diagnostics);

        ValidateVertexIncidence(
            vertices.Count,
            faceGeometries,
            diagnostics);

        double volume = CalculateVolume(
            vertices,
            faceGeometries,
            centroid);

        if (volume < settings.MinimumVolume) {
            diagnostics.Add(
                BrushValidationDiagnosticCodes.VolumeTooSmall,
                DiagnosticSeverity.Error,
                $"Brush volume {volume:G17} is below the required minimum of {settings.MinimumVolume:G17}.");
        }

        bool isClosed =
            faceGeometries.Count == brush.FaceCount &&
            edgeValidation.InvalidEdgeCount == 0 &&
            faceGeometries.All(
                face =>
                    face.Area >= settings.MinimumFaceArea);

        ConvexBrushGeometry geometry = new(
            brush,
            vertices,
            faceGeometries,
            bounds,
            centroid,
            volume,
            edgeValidation.EdgeCount,
            isClosed);

        return new BrushValidationResult(
            geometry,
            diagnostics.Snapshot());
    }

    private static List<Vector3d> ReconstructVertices(
        ConvexBrush brush,
        BrushValidationSettings settings)
    {
        List<Vector3d> vertices = [];

        for (
            int firstIndex = 0;
            firstIndex < brush.FaceCount - 2;
            firstIndex++
        ) {
            for (
                int secondIndex = firstIndex + 1;
                secondIndex < brush.FaceCount - 1;
                secondIndex++
            ) {
                for (
                    int thirdIndex = secondIndex + 1;
                    thirdIndex < brush.FaceCount;
                    thirdIndex++
                ) {
                    bool intersects =
                        PlaneIntersection3d.TryIntersect(
                            brush.Faces[firstIndex].Plane,
                            brush.Faces[secondIndex].Plane,
                            brush.Faces[thirdIndex].Plane,
                            out Vector3d candidate,
                            settings.DeterminantTolerance);

                    if (!intersects) {
                        continue;
                    }

                    if (
                        !IsInsideEveryHalfSpace(
                            brush,
                            candidate,
                            settings.HalfSpaceTolerance)
                    ) {
                        continue;
                    }

                    AddUniqueVertex(
                        vertices,
                        candidate,
                        settings.VertexMergeTolerance);
                }
            }
        }

        return vertices;
    }

    private static bool IsInsideEveryHalfSpace(
        ConvexBrush brush,
        Vector3d candidate,
        double tolerance)
    {
        foreach (BrushFace face in brush.Faces) {
            if (
                face.Plane.SignedDistanceTo(candidate) >
                tolerance
            ) {
                return false;
            }
        }

        return true;
    }

    private static void AddUniqueVertex(
        List<Vector3d> vertices,
        Vector3d candidate,
        double mergeTolerance)
    {
        double squaredTolerance =
            mergeTolerance *
            mergeTolerance;

        foreach (Vector3d existing in vertices) {
            if (
                (existing - candidate).LengthSquared <=
                squaredTolerance
            ) {
                return;
            }
        }

        vertices.Add(candidate);
    }

    private static List<BrushFaceGeometry> ReconstructFaces(
        ConvexBrush brush,
        List<Vector3d> vertices,
        BrushValidationSettings settings,
        DiagnosticBag diagnostics)
    {
        List<BrushFaceGeometry> reconstructedFaces = [];

        for (
            int faceIndex = 0;
            faceIndex < brush.FaceCount;
            faceIndex++
        ) {
            BrushFace face = brush.Faces[faceIndex];

            List<int> faceVertexIndices = [];

            for (
                int vertexIndex = 0;
                vertexIndex < vertices.Count;
                vertexIndex++
            ) {
                double distance = Math.Abs(
                    face.Plane.SignedDistanceTo(
                        vertices[vertexIndex]));

                if (
                    distance <=
                    settings.PlaneMembershipTolerance
                ) {
                    faceVertexIndices.Add(vertexIndex);
                }
            }

            if (faceVertexIndices.Count < 3) {
                diagnostics.Add(
                    BrushValidationDiagnosticCodes.FaceHasTooFewVertices,
                    DiagnosticSeverity.Error,
                    $"Face {faceIndex} reconstructs to only {faceVertexIndices.Count} vertices.");

                continue;
            }

            int[] orderedIndices = OrderFaceVertices(
                vertices,
                faceVertexIndices,
                face.Plane.Normal);

            double area = CalculateFaceArea(
                vertices,
                orderedIndices,
                face.Plane.Normal);

            if (area < settings.MinimumFaceArea) {
                diagnostics.Add(
                    BrushValidationDiagnosticCodes.FaceAreaTooSmall,
                    DiagnosticSeverity.Error,
                    $"Face {faceIndex} area {area:G17} is below the required minimum of {settings.MinimumFaceArea:G17}.");
            }

            reconstructedFaces.Add(
                new BrushFaceGeometry(
                    face,
                    orderedIndices,
                    area));
        }

        return reconstructedFaces;
    }

    private static int[] OrderFaceVertices(
        List<Vector3d> vertices,
        List<int> faceVertexIndices,
        Vector3d faceNormal)
    {
        Vector3d faceCenter = Vector3d.Zero;

        foreach (int vertexIndex in faceVertexIndices) {
            faceCenter += vertices[vertexIndex];
        }

        faceCenter /= faceVertexIndices.Count;

        Vector3d referenceAxis =
            Math.Abs(faceNormal.Z) < 0.9
                ? Vector3d.UnitZ
                : Vector3d.UnitY;

        Vector3d tangent = Vector3d.Cross(
            referenceAxis,
            faceNormal).Normalize();

        Vector3d bitangent = Vector3d.Cross(
            faceNormal,
            tangent).Normalize();

        return faceVertexIndices
            .OrderBy(
                vertexIndex =>
                {
                    Vector3d offset =
                        vertices[vertexIndex] -
                        faceCenter;

                    return Math.Atan2(
                        Vector3d.Dot(
                            offset,
                            bitangent),
                        Vector3d.Dot(
                            offset,
                            tangent));
                })
            .ToArray();
    }

    private static double CalculateFaceArea(
        List<Vector3d> vertices,
        int[] orderedIndices,
        Vector3d faceNormal)
    {
        Vector3d accumulatedCross = Vector3d.Zero;

        for (
            int index = 0;
            index < orderedIndices.Length;
            index++
        ) {
            Vector3d current =
                vertices[orderedIndices[index]];

            Vector3d next =
                vertices[
                    orderedIndices[
                        (index + 1) %
                        orderedIndices.Length]];

            accumulatedCross +=
                Vector3d.Cross(current, next);
        }

        return Math.Abs(
            Vector3d.Dot(
                accumulatedCross,
                faceNormal)) * 0.5;
    }

    private static Vector3d CalculateCentroid(
        List<Vector3d> vertices)
    {
        Vector3d total = Vector3d.Zero;

        foreach (Vector3d vertex in vertices) {
            total += vertex;
        }

        return total / vertices.Count;
    }

    private static void ValidateCentroidAgainstPlanes(
        ConvexBrush brush,
        Vector3d centroid,
        BrushValidationSettings settings,
        DiagnosticBag diagnostics)
    {
        for (
            int faceIndex = 0;
            faceIndex < brush.FaceCount;
            faceIndex++
        ) {
            double signedDistance =
                brush.Faces[faceIndex]
                    .Plane
                    .SignedDistanceTo(centroid);

            if (
                signedDistance >=
                -settings.InteriorTolerance
            ) {
                diagnostics.Add(
                    BrushValidationDiagnosticCodes.CentroidNotInsideFace,
                    DiagnosticSeverity.Error,
                    $"The reconstructed centroid is not strictly inside face {faceIndex}. The face may be reversed, redundant, or degenerate.");
            }
        }
    }

    private static void ValidateBounds(
        Bounds3d bounds,
        BrushValidationSettings settings,
        DiagnosticBag diagnostics)
    {
        ValidateExtent(
            "X",
            bounds.Width,
            settings,
            diagnostics);

        ValidateExtent(
            "Y",
            bounds.Depth,
            settings,
            diagnostics);

        ValidateExtent(
            "Z",
            bounds.Height,
            settings,
            diagnostics);
    }

    private static void ValidateExtent(
        string axisName,
        double extent,
        BrushValidationSettings settings,
        DiagnosticBag diagnostics)
    {
        if (extent < settings.MinimumExtent) {
            diagnostics.Add(
                BrushValidationDiagnosticCodes.ExtentTooSmall,
                DiagnosticSeverity.Error,
                $"Brush {axisName}-axis extent {extent:G17} is below the required minimum of {settings.MinimumExtent:G17}.");
        }
    }

    private static EdgeValidation ValidateEdges(
        IReadOnlyList<BrushFaceGeometry> faces,
        DiagnosticBag diagnostics)
    {
        Dictionary<EdgeKey, int> edgeUseCounts = [];

        foreach (BrushFaceGeometry face in faces) {
            for (
                int index = 0;
                index < face.VertexIndices.Count;
                index++
            ) {
                int first =
                    face.VertexIndices[index];

                int second =
                    face.VertexIndices[
                        (index + 1) %
                        face.VertexIndices.Count];

                EdgeKey edge = new(first, second);

                edgeUseCounts.TryGetValue(
                    edge,
                    out int existingCount);

                edgeUseCounts[edge] =
                    existingCount + 1;
            }
        }

        int invalidEdgeCount =
            edgeUseCounts.Count(
                pair =>
                    pair.Value != 2);

        if (invalidEdgeCount > 0) {
            diagnostics.Add(
                BrushValidationDiagnosticCodes.OpenOrNonManifoldEdges,
                DiagnosticSeverity.Error,
                $"{invalidEdgeCount} reconstructed edges are not shared by exactly two faces.");
        }

        return new EdgeValidation(
            edgeUseCounts.Count,
            invalidEdgeCount);
    }

    private static void ValidateVertexIncidence(
        int vertexCount,
        IReadOnlyList<BrushFaceGeometry> faces,
        DiagnosticBag diagnostics)
    {
        int[] incidentFaceCounts =
            new int[vertexCount];

        foreach (BrushFaceGeometry face in faces) {
            foreach (int vertexIndex in face.VertexIndices) {
                incidentFaceCounts[vertexIndex]++;
            }
        }

        int invalidVertexCount =
            incidentFaceCounts.Count(
                count =>
                    count < 3);

        if (invalidVertexCount > 0) {
            diagnostics.Add(
                BrushValidationDiagnosticCodes.VertexHasTooFewIncidentFaces,
                DiagnosticSeverity.Error,
                $"{invalidVertexCount} reconstructed vertices belong to fewer than three faces.");
        }
    }

    private static double CalculateVolume(
        List<Vector3d> vertices,
        List<BrushFaceGeometry> faces,
        Vector3d interiorPoint)
    {
        double volume = 0.0;

        foreach (BrushFaceGeometry face in faces) {
            int firstVertexIndex =
                face.VertexIndices[0];

            Vector3d first =
                vertices[firstVertexIndex] -
                interiorPoint;

            for (
                int index = 1;
                index < face.VertexIndices.Count - 1;
                index++
            ) {
                Vector3d second =
                    vertices[
                        face.VertexIndices[index]] -
                    interiorPoint;

                Vector3d third =
                    vertices[
                        face.VertexIndices[index + 1]] -
                    interiorPoint;

                double tetrahedronVolume =
                    Math.Abs(
                        Vector3d.Dot(
                            first,
                            Vector3d.Cross(
                                second,
                                third))) / 6.0;

                volume += tetrahedronVolume;
            }
        }

        return volume;
    }

    private readonly record struct EdgeKey
    {
        public EdgeKey(
            int firstVertex,
            int secondVertex)
        {
            if (firstVertex <= secondVertex) {
                FirstVertex = firstVertex;
                SecondVertex = secondVertex;
            }
            else {
                FirstVertex = secondVertex;
                SecondVertex = firstVertex;
            }
        }

        public int FirstVertex { get; }

        public int SecondVertex { get; }
    }

    private readonly record struct EdgeValidation(
        int EdgeCount,
        int InvalidEdgeCount);
}
