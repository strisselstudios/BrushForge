using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using BrushForge.Geometry.Bounds;
using BrushForge.Generation.Foliage;

namespace BrushForge.App.Preview;

/// <summary>
/// Builds a lightweight WPF preview from the actual generated brush bounds.
/// </summary>
internal static class TreePreviewRenderer
{
    private static readonly Color TrunkColor =
        Color.FromRgb(
            118,
            77,
            45);

    private static readonly Color CanopyColor =
        Color.FromRgb(
            72,
            133,
            79);

    public static void Render(
        Viewport3D viewport,
        TreeGenerationResult result)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(result);

        Model3DGroup scene = new();

        scene.Children.Add(
            new AmbientLight(
                Color.FromRgb(
                    110,
                    110,
                    110)));

        scene.Children.Add(
            new DirectionalLight(
                Colors.White,
                new Vector3D(
                    -1.0,
                    1.0,
                    -2.0)));

        foreach (
            GeneratedTreeBrush part in
            result.Parts
        ) {
            Color color =
                part.Role == TreeBrushRole.Trunk
                    ? TrunkColor
                    : CanopyColor;

            DiffuseMaterial material =
                new(
                    new SolidColorBrush(color));

            GeometryModel3D model = new(
                CreateBoxMesh(
                    part.Bounds),
                material)
            {
                BackMaterial = material
            };

            scene.Children.Add(model);
        }

        viewport.Children.Clear();
        viewport.Children.Add(
            new ModelVisual3D
            {
                Content = scene
            });

        viewport.Camera =
            CreateCamera(
                result.Bounds);
    }

    private static PerspectiveCamera CreateCamera(
        Bounds3d bounds)
    {
        double largestDimension = Math.Max(
            bounds.Width,
            Math.Max(
                bounds.Depth,
                bounds.Height));

        double distance = Math.Max(
            128.0,
            largestDimension * 2.4);

        Point3D target = new(
            bounds.Center.X,
            bounds.Center.Y,
            bounds.Center.Z);

        Point3D position = new(
            target.X + distance,
            target.Y - distance,
            target.Z + (distance * 0.65));

        return new PerspectiveCamera
        {
            Position = position,
            LookDirection = target - position,
            UpDirection = new Vector3D(
                0.0,
                0.0,
                1.0),
            FieldOfView = 38.0,
            NearPlaneDistance = 1.0,
            FarPlaneDistance = distance * 10.0
        };
    }

    private static MeshGeometry3D CreateBoxMesh(
        Bounds3d bounds)
    {
        double minimumX = bounds.Minimum.X;
        double minimumY = bounds.Minimum.Y;
        double minimumZ = bounds.Minimum.Z;
        double maximumX = bounds.Maximum.X;
        double maximumY = bounds.Maximum.Y;
        double maximumZ = bounds.Maximum.Z;

        Point3DCollection positions = new()
        {
            new Point3D(minimumX, minimumY, minimumZ),
            new Point3D(maximumX, minimumY, minimumZ),
            new Point3D(maximumX, maximumY, minimumZ),
            new Point3D(minimumX, maximumY, minimumZ),
            new Point3D(minimumX, minimumY, maximumZ),
            new Point3D(maximumX, minimumY, maximumZ),
            new Point3D(maximumX, maximumY, maximumZ),
            new Point3D(minimumX, maximumY, maximumZ)
        };

        Int32Collection triangleIndices = new()
        {
            0, 2, 1,
            0, 3, 2,
            4, 5, 6,
            4, 6, 7,
            0, 1, 5,
            0, 5, 4,
            1, 2, 6,
            1, 6, 5,
            2, 3, 7,
            2, 7, 6,
            3, 0, 4,
            3, 4, 7
        };

        return new MeshGeometry3D
        {
            Positions = positions,
            TriangleIndices = triangleIndices
        };
    }
}
