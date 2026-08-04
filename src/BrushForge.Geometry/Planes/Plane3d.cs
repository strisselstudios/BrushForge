using BrushForge.Core.Numerics;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Planes;

/// <summary>
/// Represents a normalized plane using the equation:
/// dot(Normal, point) = Distance.
/// </summary>
public readonly record struct Plane3d
{
    public Plane3d(
        Vector3d normal,
        double distance)
    {
        if (!normal.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(normal),
                normal,
                "A plane normal must be finite.");
        }

        if (!NumericTolerances.IsFinite(distance)) {
            throw new ArgumentOutOfRangeException(
                nameof(distance),
                distance,
                "A plane distance must be finite.");
        }

        double normalLength = normal.Length;

        if (
            !NumericTolerances.IsFinite(normalLength) ||
            normalLength <= NumericTolerances.UnitVector
        ) {
            throw new ArgumentException(
                "A plane normal cannot be zero or near zero.",
                nameof(normal));
        }

        Normal = normal / normalLength;
        Distance = distance / normalLength;

        if (!NumericTolerances.IsFinite(Distance)) {
            throw new OverflowException(
                "Normalizing the plane produced a non-finite distance.");
        }
    }

    public Vector3d Normal { get; }

    public double Distance { get; }

    public static Plane3d FromPoints(
        Vector3d first,
        Vector3d second,
        Vector3d third)
    {
        ValidatePoint(first, nameof(first));
        ValidatePoint(second, nameof(second));
        ValidatePoint(third, nameof(third));

        Vector3d firstEdge = second - first;
        Vector3d secondEdge = third - first;
        Vector3d unnormalizedNormal = Vector3d.Cross(
            firstEdge,
            secondEdge);

        if (
            !unnormalizedNormal.IsFinite ||
            unnormalizedNormal.Length <= NumericTolerances.UnitVector
        ) {
            throw new ArgumentException(
                "The three plane points must be distinct and non-collinear.");
        }

        double distance = Vector3d.Dot(
            unnormalizedNormal,
            first);

        return new Plane3d(
            unnormalizedNormal,
            distance);
    }

    public double SignedDistanceTo(Vector3d point)
    {
        ValidatePoint(point, nameof(point));

        return Vector3d.Dot(Normal, point) - Distance;
    }

    public Vector3d ProjectPoint(Vector3d point)
    {
        double signedDistance = SignedDistanceTo(point);

        return point - (Normal * signedDistance);
    }

    public Plane3d Flip()
    {
        return new Plane3d(
            -Normal,
            -Distance);
    }

    public bool IsParallelTo(
        Plane3d other,
        double tolerance = NumericTolerances.UnitVector)
    {
        ValidateTolerance(tolerance);

        double absoluteDot = Math.Abs(
            Vector3d.Dot(Normal, other.Normal));

        return NumericTolerances.NearlyEqual(
            absoluteDot,
            1.0,
            tolerance);
    }

    public bool NearlyEquals(
        Plane3d other,
        double normalTolerance = NumericTolerances.UnitVector,
        double distanceTolerance = NumericTolerances.PlaneDistance)
    {
        ValidateTolerance(normalTolerance);
        ValidateTolerance(distanceTolerance);

        return
            Normal.NearlyEquals(
                other.Normal,
                normalTolerance) &&
            NumericTolerances.NearlyEqual(
                Distance,
                other.Distance,
                distanceTolerance);
    }

    private static void ValidatePoint(
        Vector3d point,
        string parameterName)
    {
        if (!point.IsFinite) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                point,
                "Plane points must be finite.");
        }
    }

    private static void ValidateTolerance(double tolerance)
    {
        if (
            !NumericTolerances.IsFinite(tolerance) ||
            tolerance < 0.0
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                tolerance,
                "Tolerance must be finite and non-negative.");
        }
    }
}
