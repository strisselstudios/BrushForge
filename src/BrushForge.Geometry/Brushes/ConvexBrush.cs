using System.Collections.ObjectModel;
using BrushForge.Geometry.Planes;

namespace BrushForge.Geometry.Brushes;

/// <summary>
/// Immutable collection of oriented faces intended to describe one convex
/// brush. Full geometric convexity and closure validation are implemented by
/// the dedicated validation layer, not by this structural container.
/// </summary>
public sealed class ConvexBrush
{
    private readonly ReadOnlyCollection<BrushFace> _faces;

    public ConvexBrush(IEnumerable<BrushFace> faces)
    {
        ArgumentNullException.ThrowIfNull(faces);

        BrushFace[] faceArray = faces.ToArray();

        if (faceArray.Length < 4) {
            throw new ArgumentException(
                "A closed three-dimensional brush requires at least four faces.",
                nameof(faces));
        }

        if (faceArray.Any(face => face is null)) {
            throw new ArgumentException(
                "A brush cannot contain null faces.",
                nameof(faces));
        }

        for (
            int firstIndex = 0;
            firstIndex < faceArray.Length;
            firstIndex++
        ) {
            Plane3d firstPlane =
                faceArray[firstIndex].Plane;

            for (
                int secondIndex = firstIndex + 1;
                secondIndex < faceArray.Length;
                secondIndex++
            ) {
                Plane3d secondPlane =
                    faceArray[secondIndex].Plane;

                if (firstPlane.NearlyEquals(secondPlane)) {
                    throw new ArgumentException(
                        "A brush cannot contain duplicate oriented face planes.",
                        nameof(faces));
                }
            }
        }

        _faces = Array.AsReadOnly(faceArray);
    }

    public IReadOnlyList<BrushFace> Faces => _faces;

    public int FaceCount => _faces.Count;

    public bool ContainsPlane(Plane3d plane)
    {
        return _faces.Any(
            face =>
                face.Plane.NearlyEquals(plane));
    }
}
