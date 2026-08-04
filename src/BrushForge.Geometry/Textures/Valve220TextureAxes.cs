using BrushForge.Core.Numerics;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Textures;

/// <summary>
/// Valve 220 U and V texture axes and the retained rotation value.
/// </summary>
public sealed record Valve220TextureAxes
{
    public Valve220TextureAxes(
        TextureAxis uAxis,
        TextureAxis vAxis,
        double rotationDegrees)
    {
        ArgumentNullException.ThrowIfNull(uAxis);
        ArgumentNullException.ThrowIfNull(vAxis);

        if (!NumericTolerances.IsFinite(rotationDegrees)) {
            throw new ArgumentOutOfRangeException(
                nameof(rotationDegrees),
                rotationDegrees,
                "Texture rotation must be finite.");
        }

        Vector3d cross = Vector3d.Cross(
            uAxis.Direction,
            vAxis.Direction);

        if (
            !cross.IsFinite ||
            cross.Length <= NumericTolerances.UnitVector
        ) {
            throw new ArgumentException(
                "Valve 220 U and V texture axes cannot be parallel.");
        }

        UAxis = uAxis;
        VAxis = vAxis;
        RotationDegrees = rotationDegrees;
    }

    public TextureAxis UAxis { get; }

    public TextureAxis VAxis { get; }

    public double RotationDegrees { get; }
}
