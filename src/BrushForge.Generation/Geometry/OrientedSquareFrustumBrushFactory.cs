using BrushForge.Core.Numerics;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Generation.Geometry;

/// <summary>
/// Creates a six-face square prism or tapered frustum aligned to an arbitrary
/// three-dimensional axis. This is the low-face-count brush primitive used by
/// future foliage branch geometry.
/// </summary>
public static class OrientedSquareFrustumBrushFactory
{
    private const double ParallelReferenceThreshold = 0.90;

    public static ConvexBrush Create(
        Vector3d startCenter,
        Vector3d endCenter,
        double startHalfExtent,
        double endHalfExtent,
        string textureName)
    {
        if (!startCenter.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(startCenter),
                startCenter,
                "The frustum start center must be finite.");
        }

        if (!endCenter.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(endCenter),
                endCenter,
                "The frustum end center must be finite.");
        }

        ValidateHalfExtent(
            startHalfExtent,
            nameof(startHalfExtent));
        ValidateHalfExtent(
            endHalfExtent,
            nameof(endHalfExtent));
        ArgumentException.ThrowIfNullOrWhiteSpace(textureName);

        Vector3d axisVector =
            endCenter - startCenter;

        if (
            !axisVector.IsFinite ||
            axisVector.Length <= NumericTolerances.Coordinate
        ) {
            throw new ArgumentException(
                "The frustum start and end centers must define a non-zero axis.",
                nameof(endCenter));
        }

        Vector3d axis =
            axisVector.Normalize();
        Vector3d reference =
            Math.Abs(
                Vector3d.Dot(
                    axis,
                    Vector3d.UnitZ)) <
                ParallelReferenceThreshold
                ? Vector3d.UnitZ
                : Vector3d.UnitY;
        Vector3d right =
            Vector3d.Cross(
                reference,
                axis)
                .Normalize();
        Vector3d up =
            Vector3d.Cross(
                axis,
                right)
                .Normalize();

        Vector3d[] startRing =
            CreateSquareRing(
                startCenter,
                right,
                up,
                startHalfExtent);
        Vector3d[] endRing =
            CreateSquareRing(
                endCenter,
                right,
                up,
                endHalfExtent);
        string normalizedTextureName =
            textureName.Trim();

        return new ConvexBrush(
        [
            CreateFace(
                startRing[0],
                startRing[3],
                startRing[1],
                normalizedTextureName),
            CreateFace(
                endRing[0],
                endRing[1],
                endRing[3],
                normalizedTextureName),
            CreateFace(
                startRing[0],
                startRing[1],
                endRing[0],
                normalizedTextureName),
            CreateFace(
                startRing[1],
                startRing[2],
                endRing[1],
                normalizedTextureName),
            CreateFace(
                startRing[2],
                startRing[3],
                endRing[2],
                normalizedTextureName),
            CreateFace(
                startRing[3],
                startRing[0],
                endRing[3],
                normalizedTextureName)
        ]);
    }

    private static void ValidateHalfExtent(
        double halfExtent,
        string parameterName)
    {
        if (
            !NumericTolerances.IsFinite(halfExtent) ||
            halfExtent <= NumericTolerances.Coordinate
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                halfExtent,
                "A frustum half extent must be finite and greater than zero.");
        }
    }

    private static Vector3d[] CreateSquareRing(
        Vector3d center,
        Vector3d right,
        Vector3d up,
        double halfExtent)
    {
        Vector3d rightOffset =
            right * halfExtent;
        Vector3d upOffset =
            up * halfExtent;

        return
        [
            center - rightOffset - upOffset,
            center + rightOffset - upOffset,
            center + rightOffset + upOffset,
            center - rightOffset + upOffset
        ];
    }

    private static BrushFace CreateFace(
        Vector3d first,
        Vector3d second,
        Vector3d third,
        string textureName)
    {
        return BrushFace.CreateWithGeneratedAxes(
            new PlanePoints3d(
                first,
                second,
                third),
            textureName);
    }
}
