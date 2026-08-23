using BrushForge.Core.Numerics;

namespace BrushForge.Generation.Foliage.Branches;

/// <summary>
/// One stable, normalized branch in a deterministic tree skeleton.
/// Geometry is resolved later from these relative structural parameters.
/// </summary>
public sealed class PlannedTreeBranch
{
    public PlannedTreeBranch(
        string path,
        string? parentPath,
        int depth,
        double attachmentFraction,
        double azimuthDegrees,
        double elevationDegrees,
        double lengthScale,
        double startRadiusScale,
        double endRadiusScale,
        double requiredDetail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (depth < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(depth),
                depth,
                "Branch depth cannot be negative.");
        }

        if (depth == 0 && parentPath is not null) {
            throw new ArgumentException(
                "A primary branch cannot have a parent branch path.",
                nameof(parentPath));
        }

        if (depth > 0) {
            ArgumentException.ThrowIfNullOrWhiteSpace(parentPath);
        }

        ValidateUnitInterval(
            attachmentFraction,
            nameof(attachmentFraction));

        if (
            !NumericTolerances.IsFinite(azimuthDegrees) ||
            azimuthDegrees < -180.0 ||
            azimuthDegrees >= 180.0
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(azimuthDegrees),
                azimuthDegrees,
                "Branch azimuth must be finite and in the range [-180, 180).");
        }

        if (
            !NumericTolerances.IsFinite(elevationDegrees) ||
            elevationDegrees < -90.0 ||
            elevationDegrees > 90.0
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(elevationDegrees),
                elevationDegrees,
                "Branch elevation must be finite and in the range [-90, 90].");
        }

        ValidatePositiveScale(
            lengthScale,
            nameof(lengthScale));
        ValidatePositiveScale(
            startRadiusScale,
            nameof(startRadiusScale));
        ValidatePositiveScale(
            endRadiusScale,
            nameof(endRadiusScale));

        if (endRadiusScale > startRadiusScale) {
            throw new ArgumentOutOfRangeException(
                nameof(endRadiusScale),
                endRadiusScale,
                "A branch end radius scale cannot exceed its start radius scale.");
        }

        ValidateUnitInterval(
            requiredDetail,
            nameof(requiredDetail));

        Path = path.Trim();
        ParentPath = parentPath?.Trim();
        Depth = depth;
        AttachmentFraction = attachmentFraction;
        AzimuthDegrees = azimuthDegrees;
        ElevationDegrees = elevationDegrees;
        LengthScale = lengthScale;
        StartRadiusScale = startRadiusScale;
        EndRadiusScale = endRadiusScale;
        RequiredDetail = requiredDetail;
    }

    public string Path { get; }

    public string? ParentPath { get; }

    public int Depth { get; }

    /// <summary>
    /// Position along the parent axis, or along the trunk for primary branches.
    /// </summary>
    public double AttachmentFraction { get; }

    /// <summary>
    /// Local azimuth around the parent axis, in normalized degrees.
    /// </summary>
    public double AzimuthDegrees { get; }

    /// <summary>
    /// Local elevation away from the parent axis, in degrees.
    /// </summary>
    public double ElevationDegrees { get; }

    /// <summary>
    /// Branch length relative to its parent structural scale.
    /// </summary>
    public double LengthScale { get; }

    /// <summary>
    /// Branch start radius relative to its parent structural scale.
    /// </summary>
    public double StartRadiusScale { get; }

    /// <summary>
    /// Branch end radius relative to its parent structural scale.
    /// </summary>
    public double EndRadiusScale { get; }

    /// <summary>
    /// Stable Detail threshold at which this branch may later be realized.
    /// The branch remains part of the skeleton below this threshold.
    /// </summary>
    public double RequiredDetail { get; }

    private static void ValidateUnitInterval(
        double value,
        string parameterName)
    {
        if (
            !NumericTolerances.IsFinite(value) ||
            value < 0.0 ||
            value > 1.0
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and in the range [0, 1].");
        }
    }

    private static void ValidatePositiveScale(
        double value,
        string parameterName)
    {
        if (
            !NumericTolerances.IsFinite(value) ||
            value <= 0.0 ||
            value > 1.0
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The scale must be finite and in the range (0, 1].");
        }
    }
}
