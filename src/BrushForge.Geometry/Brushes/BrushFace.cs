using BrushForge.Geometry.Planes;
using BrushForge.Geometry.Textures;

namespace BrushForge.Geometry.Brushes;

/// <summary>
/// Immutable Valve 220-compatible brush-face representation.
/// </summary>
public sealed record BrushFace
{
    public const int MaximumTextureNameLength = 255;

    public BrushFace(
        PlanePoints3d planePoints,
        string textureName,
        Valve220TextureAxes textureAxes)
    {
        ArgumentNullException.ThrowIfNull(textureName);
        ArgumentNullException.ThrowIfNull(textureAxes);

        string normalizedTextureName =
            textureName.Trim();

        if (normalizedTextureName.Length == 0) {
            throw new ArgumentException(
                "A brush face requires a texture name.",
                nameof(textureName));
        }

        if (
            normalizedTextureName.Length >
            MaximumTextureNameLength
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(textureName),
                normalizedTextureName.Length,
                $"Texture names cannot exceed {MaximumTextureNameLength} characters.");
        }

        if (
            normalizedTextureName.Any(
                character =>
                    char.IsWhiteSpace(character) ||
                    char.IsControl(character) ||
                    character is '"' or '{' or '}')
        ) {
            throw new ArgumentException(
                "Texture names cannot contain whitespace, control characters, quotes, or braces.",
                nameof(textureName));
        }

        PlanePoints = planePoints;
        Plane = planePoints.Plane;
        TextureName = normalizedTextureName;
        TextureAxes = textureAxes;
    }

    public PlanePoints3d PlanePoints { get; }

    public Plane3d Plane { get; }

    public string TextureName { get; }

    public Valve220TextureAxes TextureAxes { get; }

    public static BrushFace CreateWithGeneratedAxes(
        PlanePoints3d planePoints,
        string textureName,
        double textureScale = 1.0,
        double rotationDegrees = 0.0,
        double uOffset = 0.0,
        double vOffset = 0.0)
    {
        Valve220TextureAxes textureAxes =
            TextureAxisGenerator.Generate(
                planePoints.Plane,
                textureScale,
                rotationDegrees,
                uOffset,
                vOffset);

        return new BrushFace(
            planePoints,
            textureName,
            textureAxes);
    }
}
