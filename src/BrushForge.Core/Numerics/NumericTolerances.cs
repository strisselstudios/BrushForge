namespace BrushForge.Core.Numerics;

/// <summary>
/// Central numeric tolerances used by BrushForge geometry and map processing.
/// These values are expressed in BrushForge map units.
/// </summary>
public static class NumericTolerances
{
    public const double Coordinate = 0.000001;
    public const double UnitVector = 0.000000001;
    public const double PlaneDistance = 0.00001;
    public const double GridAlignment = 0.000001;

    public static bool IsFinite(double value)
    {
        return double.IsFinite(value);
    }

    public static bool NearlyEqual(
        double left,
        double right,
        double tolerance = Coordinate)
    {
        ValidateTolerance(tolerance);

        if (!IsFinite(left) || !IsFinite(right)) {
            return false;
        }

        return Math.Abs(left - right) <= tolerance;
    }

    public static bool IsNearlyZero(
        double value,
        double tolerance = Coordinate)
    {
        return NearlyEqual(value, 0.0, tolerance);
    }

    private static void ValidateTolerance(double tolerance)
    {
        if (!IsFinite(tolerance) || tolerance < 0.0) {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                tolerance,
                "Tolerance must be finite and non-negative.");
        }
    }
}
