using BrushForge.Core.Numerics;
using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Generation.Geometry;

/// <summary>
/// Creates six-face axis-aligned convex brushes from finite bounds.
/// </summary>
public static class AxisAlignedBoxBrushFactory
{
    public static ConvexBrush Create(
        Bounds3d bounds,
        string textureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textureName);

        if (
            bounds.Width <= NumericTolerances.Coordinate ||
            bounds.Depth <= NumericTolerances.Coordinate ||
            bounds.Height <= NumericTolerances.Coordinate
        ) {
            throw new ArgumentException(
                "Axis-aligned brush bounds must have positive width, depth, and height.",
                nameof(bounds));
        }

        string normalizedTextureName =
            textureName.Trim();

        double minimumX = bounds.Minimum.X;
        double minimumY = bounds.Minimum.Y;
        double minimumZ = bounds.Minimum.Z;
        double maximumX = bounds.Maximum.X;
        double maximumY = bounds.Maximum.Y;
        double maximumZ = bounds.Maximum.Z;

        return new ConvexBrush(
        [
            CreateFace(
                new Vector3d(minimumX, minimumY, minimumZ),
                new Vector3d(minimumX, maximumY, minimumZ),
                new Vector3d(maximumX, minimumY, minimumZ),
                normalizedTextureName),

            CreateFace(
                new Vector3d(minimumX, minimumY, maximumZ),
                new Vector3d(maximumX, minimumY, maximumZ),
                new Vector3d(minimumX, maximumY, maximumZ),
                normalizedTextureName),

            CreateFace(
                new Vector3d(minimumX, minimumY, minimumZ),
                new Vector3d(minimumX, minimumY, maximumZ),
                new Vector3d(minimumX, maximumY, minimumZ),
                normalizedTextureName),

            CreateFace(
                new Vector3d(maximumX, minimumY, minimumZ),
                new Vector3d(maximumX, maximumY, minimumZ),
                new Vector3d(maximumX, minimumY, maximumZ),
                normalizedTextureName),

            CreateFace(
                new Vector3d(minimumX, minimumY, minimumZ),
                new Vector3d(maximumX, minimumY, minimumZ),
                new Vector3d(minimumX, minimumY, maximumZ),
                normalizedTextureName),

            CreateFace(
                new Vector3d(minimumX, maximumY, minimumZ),
                new Vector3d(minimumX, maximumY, maximumZ),
                new Vector3d(maximumX, maximumY, minimumZ),
                normalizedTextureName)
        ]);
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
