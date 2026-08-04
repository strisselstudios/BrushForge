using BrushForge.Core.Numerics;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Textures;

/// <summary>
/// One Valve 220 texture axis with an offset and independent texture scale.
/// </summary>
public sealed record TextureAxis
{
    public TextureAxis(
        Vector3d direction,
        double offset,
        double scale)
    {
        if (!direction.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "A texture-axis direction must be finite.");
        }

        if (
            direction.Length <=
            NumericTolerances.UnitVector
        ) {
            throw new ArgumentException(
                "A texture-axis direction cannot be zero or near zero.",
                nameof(direction));
        }

        if (!NumericTolerances.IsFinite(offset)) {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "A texture-axis offset must be finite.");
        }

        if (
            !NumericTolerances.IsFinite(scale) ||
            NumericTolerances.IsNearlyZero(
                scale,
                NumericTolerances.UnitVector)
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(scale),
                scale,
                "A texture-axis scale must be finite and non-zero.");
        }

        Direction = direction;
        Offset = offset;
        Scale = scale;
    }

    public Vector3d Direction { get; }

    public double Offset { get; }

    public double Scale { get; }
}
