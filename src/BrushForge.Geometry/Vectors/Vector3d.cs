using BrushForge.Core.Numerics;

namespace BrushForge.Geometry.Vectors;

/// <summary>
/// Immutable double-precision three-dimensional vector.
/// </summary>
public readonly record struct Vector3d(
    double X,
    double Y,
    double Z)
{
    public static Vector3d Zero { get; } = new(0.0, 0.0, 0.0);
    public static Vector3d One { get; } = new(1.0, 1.0, 1.0);
    public static Vector3d UnitX { get; } = new(1.0, 0.0, 0.0);
    public static Vector3d UnitY { get; } = new(0.0, 1.0, 0.0);
    public static Vector3d UnitZ { get; } = new(0.0, 0.0, 1.0);

    public bool IsFinite =>
        NumericTolerances.IsFinite(X) &&
        NumericTolerances.IsFinite(Y) &&
        NumericTolerances.IsFinite(Z);

    public double LengthSquared =>
        (X * X) +
        (Y * Y) +
        (Z * Z);

    public double Length =>
        Math.Sqrt(LengthSquared);

    public Vector3d Normalize()
    {
        if (!IsFinite) {
            throw new InvalidOperationException(
                "A non-finite vector cannot be normalized.");
        }

        double length = Length;

        if (
            !NumericTolerances.IsFinite(length) ||
            length <= NumericTolerances.UnitVector
        ) {
            throw new InvalidOperationException(
                "A zero-length or near-zero-length vector cannot be normalized.");
        }

        return this / length;
    }

    public bool NearlyEquals(
        Vector3d other,
        double tolerance = NumericTolerances.Coordinate)
    {
        return
            NumericTolerances.NearlyEqual(X, other.X, tolerance) &&
            NumericTolerances.NearlyEqual(Y, other.Y, tolerance) &&
            NumericTolerances.NearlyEqual(Z, other.Z, tolerance);
    }

    public double DistanceTo(Vector3d other)
    {
        return (this - other).Length;
    }

    public static double Dot(
        Vector3d left,
        Vector3d right)
    {
        return
            (left.X * right.X) +
            (left.Y * right.Y) +
            (left.Z * right.Z);
    }

    public static Vector3d Cross(
        Vector3d left,
        Vector3d right)
    {
        return new Vector3d(
            (left.Y * right.Z) - (left.Z * right.Y),
            (left.Z * right.X) - (left.X * right.Z),
            (left.X * right.Y) - (left.Y * right.X));
    }

    public static Vector3d operator +(
        Vector3d left,
        Vector3d right)
    {
        return new Vector3d(
            left.X + right.X,
            left.Y + right.Y,
            left.Z + right.Z);
    }

    public static Vector3d operator -(
        Vector3d left,
        Vector3d right)
    {
        return new Vector3d(
            left.X - right.X,
            left.Y - right.Y,
            left.Z - right.Z);
    }

    public static Vector3d operator -(Vector3d value)
    {
        return new Vector3d(
            -value.X,
            -value.Y,
            -value.Z);
    }

    public static Vector3d operator *(
        Vector3d value,
        double scalar)
    {
        return new Vector3d(
            value.X * scalar,
            value.Y * scalar,
            value.Z * scalar);
    }

    public static Vector3d operator *(
        double scalar,
        Vector3d value)
    {
        return value * scalar;
    }

    public static Vector3d operator /(
        Vector3d value,
        double divisor)
    {
        if (
            !NumericTolerances.IsFinite(divisor) ||
            NumericTolerances.IsNearlyZero(
                divisor,
                NumericTolerances.UnitVector)
        ) {
            throw new DivideByZeroException(
                "A vector divisor must be finite and non-zero.");
        }

        return new Vector3d(
            value.X / divisor,
            value.Y / divisor,
            value.Z / divisor);
    }
}
