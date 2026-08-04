using BrushForge.Core.Numerics;
using BrushForge.Geometry.Planes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Intersections;

/// <summary>
/// Calculates intersections between three normalized planes.
/// </summary>
public static class PlaneIntersection3d
{
    public static bool TryIntersect(
        Plane3d first,
        Plane3d second,
        Plane3d third,
        out Vector3d intersection,
        double determinantTolerance = NumericTolerances.UnitVector)
    {
        ValidateTolerance(determinantTolerance);

        Vector3d secondCrossThird = Vector3d.Cross(
            second.Normal,
            third.Normal);

        double determinant = Vector3d.Dot(
            first.Normal,
            secondCrossThird);

        if (
            !NumericTolerances.IsFinite(determinant) ||
            Math.Abs(determinant) <= determinantTolerance
        ) {
            intersection = default;
            return false;
        }

        Vector3d thirdCrossFirst = Vector3d.Cross(
            third.Normal,
            first.Normal);

        Vector3d firstCrossSecond = Vector3d.Cross(
            first.Normal,
            second.Normal);

        Vector3d numerator =
            (secondCrossThird * first.Distance) +
            (thirdCrossFirst * second.Distance) +
            (firstCrossSecond * third.Distance);

        Vector3d candidate = numerator / determinant;

        if (!candidate.IsFinite) {
            intersection = default;
            return false;
        }

        intersection = candidate;
        return true;
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
                "The determinant tolerance must be finite and non-negative.");
        }
    }
}
