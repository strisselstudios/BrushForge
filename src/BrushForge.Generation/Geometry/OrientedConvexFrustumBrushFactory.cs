using BrushForge.Core.Numerics;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Generation.Geometry;

/// <summary>
/// Creates a regular convex prism or tapered frustum aligned to an arbitrary
/// three-dimensional axis. The supplied apothem is the perpendicular distance
/// from the centerline to each side plane, so changing side count preserves the
/// visible branch thickness instead of shrinking the cross-section.
/// </summary>
public static class OrientedConvexFrustumBrushFactory
{
    public const int MinimumSideCount = 4;
    public const int MaximumSideCount = 16;

    private const double ParallelReferenceThreshold = 0.90;

    public static ConvexBrush Create(
        Vector3d startCenter,
        Vector3d endCenter,
        double startApothem,
        double endApothem,
        int sideCount,
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

        ValidateApothem(
            startApothem,
            nameof(startApothem));
        ValidateApothem(
            endApothem,
            nameof(endApothem));

        if (
            sideCount < MinimumSideCount ||
            sideCount > MaximumSideCount
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(sideCount),
                sideCount,
                $"The frustum side count must be between {MinimumSideCount} and {MaximumSideCount}.");
        }

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
        (Vector3d right, Vector3d up) =
            CreatePerpendicularBasis(
                axis);
        Vector3d[] startRing =
            CreateRegularRing(
                startCenter,
                right,
                up,
                startApothem,
                sideCount);
        Vector3d[] endRing =
            CreateRegularRing(
                endCenter,
                right,
                up,
                endApothem,
                sideCount);
        string normalizedTextureName =
            textureName.Trim();
        BrushFace[] faces =
            new BrushFace[sideCount + 2];

        faces[0] =
            CreateStartCap(
                startCenter,
                right,
                up,
                startApothem,
                normalizedTextureName);
        faces[1] =
            CreateEndCap(
                endCenter,
                right,
                up,
                endApothem,
                normalizedTextureName);

        for (
            int sideIndex = 0;
            sideIndex < sideCount;
            sideIndex++
        ) {
            int nextIndex =
                (sideIndex + 1) %
                sideCount;

            faces[sideIndex + 2] =
                CreateFace(
                    startRing[sideIndex],
                    startRing[nextIndex],
                    endRing[sideIndex],
                    normalizedTextureName);
        }

        return new ConvexBrush(faces);
    }

    private static BrushFace CreateStartCap(
        Vector3d center,
        Vector3d right,
        Vector3d up,
        double apothem,
        string textureName)
    {
        Vector3d second =
            center +
            (right * apothem);
        Vector3d third =
            center -
            (right * apothem);
        Vector3d first =
            second +
            (up * apothem * 2.0);

        return CreateFace(
            first,
            second,
            third,
            textureName);
    }

    private static BrushFace CreateEndCap(
        Vector3d center,
        Vector3d right,
        Vector3d up,
        double apothem,
        string textureName)
    {
        Vector3d second =
            center +
            (right * apothem);
        Vector3d third =
            center -
            (right * apothem);
        Vector3d first =
            second -
            (up * apothem * 2.0);

        return CreateFace(
            first,
            second,
            third,
            textureName);
    }

    private static Vector3d[] CreateRegularRing(
        Vector3d center,
        Vector3d right,
        Vector3d up,
        double apothem,
        int sideCount)
    {
        double halfCentralAngle =
            Math.PI /
            sideCount;
        double circumradius =
            apothem /
            Math.Cos(
                halfCentralAngle);
        double angleStep =
            (Math.PI * 2.0) /
            sideCount;
        double startingAngle =
            -Math.PI +
            halfCentralAngle;
        Vector3d[] ring =
            new Vector3d[sideCount];

        for (
            int vertexIndex = 0;
            vertexIndex < sideCount;
            vertexIndex++
        ) {
            double angle =
                startingAngle +
                (angleStep * vertexIndex);

            ring[vertexIndex] =
                center +
                (
                    right *
                    (Math.Cos(angle) * circumradius)
                ) +
                (
                    up *
                    (Math.Sin(angle) * circumradius)
                );
        }

        return ring;
    }

    private static (
        Vector3d Right,
        Vector3d Up) CreatePerpendicularBasis(
        Vector3d axis)
    {
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

        return (
            right,
            up);
    }

    private static void ValidateApothem(
        double apothem,
        string parameterName)
    {
        if (
            !NumericTolerances.IsFinite(apothem) ||
            apothem <= NumericTolerances.Coordinate
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                apothem,
                "A frustum apothem must be finite and greater than zero.");
        }
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
