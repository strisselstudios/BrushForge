using BrushForge.Geometry.Planes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Brushes;

/// <summary>
/// Three ordered, non-collinear points defining an oriented brush plane.
/// Point order determines the direction of the face normal.
/// </summary>
public readonly record struct PlanePoints3d
{
    public PlanePoints3d(
        Vector3d first,
        Vector3d second,
        Vector3d third)
    {
        if (!first.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(first),
                first,
                "Plane points must be finite.");
        }

        if (!second.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(second),
                second,
                "Plane points must be finite.");
        }

        if (!third.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(third),
                third,
                "Plane points must be finite.");
        }

        Plane3d plane = Plane3d.FromPoints(
            first,
            second,
            third);

        First = first;
        Second = second;
        Third = third;
        Plane = plane;
    }

    public Vector3d First { get; }

    public Vector3d Second { get; }

    public Vector3d Third { get; }

    public Plane3d Plane { get; }
}
