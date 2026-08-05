using BrushForge.Core.Numerics;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Generation.Geometry;

/// <summary>
/// Creates a vertical convex prism or frustum from matching horizontal rings.
/// Ring vertices must be ordered counterclockwise when viewed from above.
/// </summary>
public static class VerticalConvexFrustumBrushFactory
{
    public const int MinimumSideCount = 3;
    public const int MaximumSideCount = 32;

    public static ConvexBrush Create(
        IReadOnlyList<Vector3d> bottomRing,
        IReadOnlyList<Vector3d> topRing,
        string textureName)
    {
        ArgumentNullException.ThrowIfNull(bottomRing);
        ArgumentNullException.ThrowIfNull(topRing);
        ArgumentException.ThrowIfNullOrWhiteSpace(textureName);

        Vector3d[] bottomVertices =
            bottomRing.ToArray();
        Vector3d[] topVertices =
            topRing.ToArray();

        ValidateSideCounts(
            bottomVertices,
            topVertices);
        ValidateFiniteVertices(
            bottomVertices,
            nameof(bottomRing));
        ValidateFiniteVertices(
            topVertices,
            nameof(topRing));

        double bottomZ =
            ValidateHorizontalRing(
                bottomVertices,
                nameof(bottomRing));
        double topZ =
            ValidateHorizontalRing(
                topVertices,
                nameof(topRing));

        if (
            topZ - bottomZ <=
            NumericTolerances.Coordinate
        ) {
            throw new ArgumentException(
                "The top ring must be above the bottom ring.",
                nameof(topRing));
        }

        ValidateCounterClockwiseConvexRing(
            bottomVertices,
            nameof(bottomRing));
        ValidateCounterClockwiseConvexRing(
            topVertices,
            nameof(topRing));

        string normalizedTextureName =
            textureName.Trim();
        BrushFace[] faces =
            new BrushFace[bottomVertices.Length + 2];

        faces[0] = CreateFace(
            bottomVertices[0],
            bottomVertices[2],
            bottomVertices[1],
            normalizedTextureName);
        faces[1] = CreateFace(
            topVertices[0],
            topVertices[1],
            topVertices[2],
            normalizedTextureName);

        for (
            int sideIndex = 0;
            sideIndex < bottomVertices.Length;
            sideIndex++
        ) {
            int nextIndex =
                (sideIndex + 1) %
                bottomVertices.Length;
            PlanePoints3d sidePlanePoints = new(
                bottomVertices[sideIndex],
                bottomVertices[nextIndex],
                topVertices[sideIndex]);

            double fourthPointDistance =
                Math.Abs(
                    sidePlanePoints.Plane.SignedDistanceTo(
                        topVertices[nextIndex]));

            if (
                fourthPointDistance >
                NumericTolerances.PlaneDistance
            ) {
                throw new ArgumentException(
                    "Corresponding ring edges must form planar frustum sides.",
                    nameof(topRing));
            }

            faces[sideIndex + 2] =
                BrushFace.CreateWithGeneratedAxes(
                    sidePlanePoints,
                    normalizedTextureName);
        }

        return new ConvexBrush(faces);
    }

    private static void ValidateSideCounts(
        Vector3d[] bottomVertices,
        Vector3d[] topVertices)
    {
        if (
            bottomVertices.Length < MinimumSideCount ||
            bottomVertices.Length > MaximumSideCount
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(bottomVertices),
                bottomVertices.Length,
                $"Frustum rings must contain between {MinimumSideCount} and {MaximumSideCount} vertices.");
        }

        if (topVertices.Length != bottomVertices.Length) {
            throw new ArgumentException(
                "The bottom and top rings must contain the same number of vertices.",
                nameof(topVertices));
        }
    }

    private static void ValidateFiniteVertices(
        Vector3d[] vertices,
        string parameterName)
    {
        if (vertices.Any(vertex => !vertex.IsFinite)) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Frustum ring vertices must be finite.");
        }
    }

    private static double ValidateHorizontalRing(
        Vector3d[] vertices,
        string parameterName)
    {
        double ringZ =
            vertices[0].Z;

        foreach (Vector3d vertex in vertices) {
            if (
                !NumericTolerances.NearlyEqual(
                    vertex.Z,
                    ringZ,
                    NumericTolerances.PlaneDistance)
            ) {
                throw new ArgumentException(
                    "Each frustum ring must lie on one horizontal plane.",
                    parameterName);
            }
        }

        return ringZ;
    }

    private static void ValidateCounterClockwiseConvexRing(
        Vector3d[] vertices,
        string parameterName)
    {
        double signedTwiceArea = 0.0;

        for (
            int index = 0;
            index < vertices.Length;
            index++
        ) {
            Vector3d current =
                vertices[index];
            Vector3d next =
                vertices[(index + 1) % vertices.Length];

            signedTwiceArea +=
                (current.X * next.Y) -
                (next.X * current.Y);
        }

        if (
            signedTwiceArea <=
            NumericTolerances.Coordinate
        ) {
            throw new ArgumentException(
                "Frustum rings must be non-degenerate and counterclockwise when viewed from above.",
                parameterName);
        }

        for (
            int index = 0;
            index < vertices.Length;
            index++
        ) {
            Vector3d previous =
                vertices[
                    (index - 1 + vertices.Length) %
                    vertices.Length];
            Vector3d current =
                vertices[index];
            Vector3d next =
                vertices[(index + 1) % vertices.Length];
            double turn =
                ((current.X - previous.X) *
                    (next.Y - current.Y)) -
                ((current.Y - previous.Y) *
                    (next.X - current.X));

            if (turn <= NumericTolerances.Coordinate) {
                throw new ArgumentException(
                    "Frustum rings must be strictly convex and cannot contain duplicate or collinear vertices.",
                    parameterName);
            }
        }
    }

    private static BrushFace CreateFace(
        Vector3d first,
        Vector3d second,
        Vector3d third,
        string textureName)
    {
        return BrushFace.CreateWithGeneratedAxes(
            new PlanePoints3d(
                first,
                second,
                third),
            textureName);
    }
}
