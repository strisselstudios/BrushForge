using System.Collections.ObjectModel;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Generation.Foliage;

/// <summary>
/// Immutable generated trunk geometry and the center of its upper ring.
/// </summary>
internal sealed class GeneratedTrunk
{
    private readonly ReadOnlyCollection<GeneratedTreeBrush> _parts;

    public GeneratedTrunk(
        IEnumerable<GeneratedTreeBrush> parts,
        Vector3d topCenter,
        TrunkAttachmentProfile attachmentProfile)
    {
        ArgumentNullException.ThrowIfNull(parts);

        GeneratedTreeBrush[] partArray =
            parts.ToArray();

        if (partArray.Length == 0) {
            throw new ArgumentException(
                "A generated trunk requires at least one brush part.",
                nameof(parts));
        }

        if (
            partArray.Any(
                part =>
                    part is null ||
                    part.Role != TreeBrushRole.Trunk)
        ) {
            throw new ArgumentException(
                "Generated trunk parts must contain only non-null trunk brushes.",
                nameof(parts));
        }

        if (!topCenter.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(topCenter),
                topCenter,
                "The generated trunk top center must be finite.");
        }

        ArgumentNullException.ThrowIfNull(attachmentProfile);

        _parts =
            Array.AsReadOnly(
                partArray);
        TopCenter = topCenter;
        AttachmentProfile = attachmentProfile;
    }

    public IReadOnlyList<GeneratedTreeBrush> Parts =>
        _parts;

    public Vector3d TopCenter { get; }

    public TrunkAttachmentProfile AttachmentProfile { get; }
}
