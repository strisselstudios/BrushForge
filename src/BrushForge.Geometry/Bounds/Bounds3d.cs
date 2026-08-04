using BrushForge.Core.Numerics;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Bounds;

/// <summary>
/// Immutable finite axis-aligned three-dimensional bounds.
/// </summary>
public readonly record struct Bounds3d
{
    public Bounds3d(
        Vector3d minimum,
        Vector3d maximum)
    {
        if (!minimum.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(minimum),
                minimum,
                "Bounds coordinates must be finite.");
        }

        if (!maximum.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(maximum),
                maximum,
                "Bounds coordinates must be finite.");
        }

        if (
            minimum.X > maximum.X ||
            minimum.Y > maximum.Y ||
            minimum.Z > maximum.Z
        ) {
            throw new ArgumentException(
                "Every minimum bounds component must be less than or equal to its maximum component.");
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    public Vector3d Minimum { get; }

    public Vector3d Maximum { get; }

    public Vector3d Size => Maximum - Minimum;

    public Vector3d Center => (Minimum + Maximum) / 2.0;

    public double Width => Maximum.X - Minimum.X;

    public double Depth => Maximum.Y - Minimum.Y;

    public double Height => Maximum.Z - Minimum.Z;

    public static Bounds3d FromPoints(
        IEnumerable<Vector3d> points)
    {
        ArgumentNullException.ThrowIfNull(points);

        using IEnumerator<Vector3d> enumerator =
            points.GetEnumerator();

        if (!enumerator.MoveNext()) {
            throw new ArgumentException(
                "At least one point is required to create bounds.",
                nameof(points));
        }

        Vector3d first = enumerator.Current;

        if (!first.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(points),
                "Bounds points must be finite.");
        }

        double minimumX = first.X;
        double minimumY = first.Y;
        double minimumZ = first.Z;
        double maximumX = first.X;
        double maximumY = first.Y;
        double maximumZ = first.Z;

        while (enumerator.MoveNext()) {
            Vector3d point = enumerator.Current;

            if (!point.IsFinite) {
                throw new ArgumentOutOfRangeException(
                    nameof(points),
                    "Bounds points must be finite.");
            }

            minimumX = Math.Min(minimumX, point.X);
            minimumY = Math.Min(minimumY, point.Y);
            minimumZ = Math.Min(minimumZ, point.Z);
            maximumX = Math.Max(maximumX, point.X);
            maximumY = Math.Max(maximumY, point.Y);
            maximumZ = Math.Max(maximumZ, point.Z);
        }

        return new Bounds3d(
            new Vector3d(
                minimumX,
                minimumY,
                minimumZ),
            new Vector3d(
                maximumX,
                maximumY,
                maximumZ));
    }

    public bool Contains(
        Vector3d point,
        double tolerance = NumericTolerances.Coordinate)
    {
        ValidateTolerance(tolerance);

        if (!point.IsFinite) {
            return false;
        }

        return
            point.X >= Minimum.X - tolerance &&
            point.X <= Maximum.X + tolerance &&
            point.Y >= Minimum.Y - tolerance &&
            point.Y <= Maximum.Y + tolerance &&
            point.Z >= Minimum.Z - tolerance &&
            point.Z <= Maximum.Z + tolerance;
    }

    public bool Intersects(
        Bounds3d other,
        double tolerance = NumericTolerances.Coordinate)
    {
        ValidateTolerance(tolerance);

        return
            Maximum.X + tolerance >= other.Minimum.X &&
            Minimum.X - tolerance <= other.Maximum.X &&
            Maximum.Y + tolerance >= other.Minimum.Y &&
            Minimum.Y - tolerance <= other.Maximum.Y &&
            Maximum.Z + tolerance >= other.Minimum.Z &&
            Minimum.Z - tolerance <= other.Maximum.Z;
    }

    public Bounds3d Expand(double amount)
    {
        if (
            !NumericTolerances.IsFinite(amount) ||
            amount < 0.0
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Bounds expansion must be finite and non-negative.");
        }

        Vector3d expansion = new(
            amount,
            amount,
            amount);

        return new Bounds3d(
            Minimum - expansion,
            Maximum + expansion);
    }

    public Bounds3d Union(Bounds3d other)
    {
        return new Bounds3d(
            new Vector3d(
                Math.Min(Minimum.X, other.Minimum.X),
                Math.Min(Minimum.Y, other.Minimum.Y),
                Math.Min(Minimum.Z, other.Minimum.Z)),
            new Vector3d(
                Math.Max(Maximum.X, other.Maximum.X),
                Math.Max(Maximum.Y, other.Maximum.Y),
                Math.Max(Maximum.Z, other.Maximum.Z)));
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
