using BrushForge.Geometry.Planes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Geometry.Textures;

/// <summary>
/// Generates deterministic Valve-style axial texture projection.
/// </summary>
public static class TextureAxisGenerator
{
    private static readonly ProjectionBasis[] ProjectionBases =
    [
        new(
            Vector3d.UnitZ,
            Vector3d.UnitX,
            -Vector3d.UnitY),

        new(
            -Vector3d.UnitZ,
            Vector3d.UnitX,
            -Vector3d.UnitY),

        new(
            Vector3d.UnitX,
            Vector3d.UnitY,
            -Vector3d.UnitZ),

        new(
            -Vector3d.UnitX,
            Vector3d.UnitY,
            -Vector3d.UnitZ),

        new(
            Vector3d.UnitY,
            Vector3d.UnitX,
            -Vector3d.UnitZ),

        new(
            -Vector3d.UnitY,
            Vector3d.UnitX,
            -Vector3d.UnitZ)
    ];

    public static Valve220TextureAxes Generate(
        Plane3d plane,
        double textureScale = 1.0,
        double rotationDegrees = 0.0,
        double uOffset = 0.0,
        double vOffset = 0.0)
    {
        ProjectionBasis basis =
            FindBestProjectionBasis(plane.Normal);

        double radians =
            rotationDegrees *
            (Math.PI / 180.0);

        double cosine = Math.Cos(radians);
        double sine = Math.Sin(radians);

        Vector3d rotatedU =
            (basis.UAxis * cosine) +
            (basis.VAxis * sine);

        Vector3d rotatedV =
            (-basis.UAxis * sine) +
            (basis.VAxis * cosine);

        return new Valve220TextureAxes(
            new TextureAxis(
                rotatedU,
                uOffset,
                textureScale),
            new TextureAxis(
                rotatedV,
                vOffset,
                textureScale),
            rotationDegrees);
    }

    private static ProjectionBasis FindBestProjectionBasis(
        Vector3d faceNormal)
    {
        ProjectionBasis best = ProjectionBases[0];
        double bestDot = Vector3d.Dot(
            faceNormal,
            best.FaceNormal);

        for (
            int index = 1;
            index < ProjectionBases.Length;
            index++
        ) {
            ProjectionBasis candidate =
                ProjectionBases[index];

            double candidateDot = Vector3d.Dot(
                faceNormal,
                candidate.FaceNormal);

            if (candidateDot > bestDot) {
                best = candidate;
                bestDot = candidateDot;
            }
        }

        return best;
    }

    private readonly record struct ProjectionBasis(
        Vector3d FaceNormal,
        Vector3d UAxis,
        Vector3d VAxis);
}
