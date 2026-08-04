using BrushForge.Core.Numerics;

namespace BrushForge.Core.Coordinates;

/// <summary>
/// Converts an external source coordinate into BrushForge map units.
/// </summary>
public sealed record CoordinateScale
{
    public static CoordinateScale Identity { get; } = new(1.0);

    public CoordinateScale(double mapUnitsPerSourceUnit)
    {
        if (
            !NumericTolerances.IsFinite(mapUnitsPerSourceUnit) ||
            mapUnitsPerSourceUnit <= 0.0
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(mapUnitsPerSourceUnit),
                mapUnitsPerSourceUnit,
                "Coordinate scale must be finite and greater than zero.");
        }

        MapUnitsPerSourceUnit = mapUnitsPerSourceUnit;
    }

    public double MapUnitsPerSourceUnit { get; }

    public double ToMapUnits(double sourceUnits)
    {
        ValidateCoordinate(sourceUnits, nameof(sourceUnits));

        double result = sourceUnits * MapUnitsPerSourceUnit;

        if (!NumericTolerances.IsFinite(result)) {
            throw new OverflowException(
                "Scaling the source coordinate produced a non-finite map coordinate.");
        }

        return result;
    }

    public double FromMapUnits(double mapUnits)
    {
        ValidateCoordinate(mapUnits, nameof(mapUnits));

        double result = mapUnits / MapUnitsPerSourceUnit;

        if (!NumericTolerances.IsFinite(result)) {
            throw new OverflowException(
                "Unscaling the map coordinate produced a non-finite source coordinate.");
        }

        return result;
    }

    private static void ValidateCoordinate(double value, string parameterName)
    {
        if (!NumericTolerances.IsFinite(value)) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Coordinate values must be finite.");
        }
    }
}
