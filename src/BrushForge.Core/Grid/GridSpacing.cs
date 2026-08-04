using BrushForge.Core.Numerics;

namespace BrushForge.Core.Grid;

/// <summary>
/// Represents a positive map-grid spacing and deterministic snapping policy.
/// Midpoints are rounded away from zero.
/// </summary>
public sealed record GridSpacing
{
    public static GridSpacing One { get; } = new(1.0);
    public static GridSpacing Two { get; } = new(2.0);
    public static GridSpacing Four { get; } = new(4.0);
    public static GridSpacing Eight { get; } = new(8.0);
    public static GridSpacing Sixteen { get; } = new(16.0);
    public static GridSpacing ThirtyTwo { get; } = new(32.0);
    public static GridSpacing SixtyFour { get; } = new(64.0);

    public GridSpacing(double units)
    {
        if (!NumericTolerances.IsFinite(units) || units <= 0.0) {
            throw new ArgumentOutOfRangeException(
                nameof(units),
                units,
                "Grid spacing must be finite and greater than zero.");
        }

        Units = units;
    }

    public double Units { get; }

    public double Snap(double value)
    {
        if (!NumericTolerances.IsFinite(value)) {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Grid coordinates must be finite.");
        }

        double quotient = value / Units;
        double rounded = Math.Round(
            quotient,
            MidpointRounding.AwayFromZero);

        double result = rounded * Units;

        if (!NumericTolerances.IsFinite(result)) {
            throw new OverflowException(
                "Grid snapping produced a non-finite coordinate.");
        }

        return result == 0.0 ? 0.0 : result;
    }

    public bool IsAligned(
        double value,
        double tolerance = NumericTolerances.GridAlignment)
    {
        if (!NumericTolerances.IsFinite(value)) {
            return false;
        }

        return NumericTolerances.NearlyEqual(
            value,
            Snap(value),
            tolerance);
    }
}
