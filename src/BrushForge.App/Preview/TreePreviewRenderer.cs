using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Triangulation;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Foliage;

namespace BrushForge.App.Preview;

/// <summary>
/// Builds a lightweight WPF preview from reconstructed generated brush faces.
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
                CreateBrushMesh(
                    part.Brush),
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
    }

    private static MeshGeometry3D CreateBrushMesh(
        ConvexBrush brush)
    {
        BrushValidationResult validation =
            ConvexBrushValidator.Validate(
                brush);

        if (
            !validation.IsValid ||
            validation.Geometry is null
        ) {
            throw new InvalidOperationException(
                "A generated brush could not be reconstructed for preview rendering.");
        }

        TriangulatedBrushMesh triangulatedMesh =
            ConvexBrushTriangulator.Triangulate(
                validation.Geometry);

        Point3DCollection positions = new();

        foreach (Vector3d vertex in triangulatedMesh.Vertices) {
            positions.Add(
                new Point3D(
                    vertex.X,
                    vertex.Y,
                    vertex.Z));
        }

        Int32Collection triangleIndices = new();

        foreach (int index in triangulatedMesh.TriangleIndices) {
            triangleIndices.Add(index);
        }

        MeshGeometry3D mesh = new()
        {
            Positions = positions,
            TriangleIndices = triangleIndices
        };

        mesh.Freeze();

        return mesh;
    }
}
