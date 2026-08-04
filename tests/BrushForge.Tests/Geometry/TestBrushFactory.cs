using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Tests.Geometry;

internal static class TestBrushFactory
{
    public static ConvexBrush CreateBox(
        Vector3d minimum,
        Vector3d maximum,
        string textureName = "STONE")
    {
        double minimumX = minimum.X;
        double minimumY = minimum.Y;
        double minimumZ = minimum.Z;
        double maximumX = maximum.X;
        double maximumY = maximum.Y;
        double maximumZ = maximum.Z;

        return new ConvexBrush(
        [
            CreateFace(
                new Vector3d(minimumX, minimumY, minimumZ),
                new Vector3d(minimumX, maximumY, minimumZ),
                new Vector3d(maximumX, minimumY, minimumZ),
                textureName),

            CreateFace(
                new Vector3d(minimumX, minimumY, maximumZ),
                new Vector3d(maximumX, minimumY, maximumZ),
                new Vector3d(minimumX, maximumY, maximumZ),
                textureName),

            CreateFace(
                new Vector3d(minimumX, minimumY, minimumZ),
                new Vector3d(minimumX, minimumY, maximumZ),
                new Vector3d(minimumX, maximumY, minimumZ),
                textureName),

            CreateFace(
                new Vector3d(maximumX, minimumY, minimumZ),
                new Vector3d(maximumX, maximumY, minimumZ),
                new Vector3d(maximumX, minimumY, maximumZ),
                textureName),

            CreateFace(
                new Vector3d(minimumX, minimumY, minimumZ),
                new Vector3d(maximumX, minimumY, minimumZ),
                new Vector3d(minimumX, minimumY, maximumZ),
                textureName),

            CreateFace(
                new Vector3d(minimumX, maximumY, minimumZ),
                new Vector3d(minimumX, maximumY, maximumZ),
                new Vector3d(maximumX, maximumY, minimumZ),
                textureName)
        ]);
    }

    public static ConvexBrush CreateTetrahedron(
        double size = 64.0,
        string textureName = "STONE")
    {
        Vector3d origin = Vector3d.Zero;
        Vector3d x = new(size, 0.0, 0.0);
        Vector3d y = new(0.0, size, 0.0);
        Vector3d z = new(0.0, 0.0, size);

        return new ConvexBrush(
        [
            CreateFace(origin, y, x, textureName),
            CreateFace(origin, z, y, textureName),
            CreateFace(origin, x, z, textureName),
            CreateFace(x, y, z, textureName)
        ]);
    }

    public static BrushFace CreateFace(
        Vector3d first,
        Vector3d second,
        Vector3d third,
        string textureName = "STONE")
    {
        return BrushFace.CreateWithGeneratedAxes(
            new PlanePoints3d(
                first,
                second,
                third),
            textureName);
    }
}
