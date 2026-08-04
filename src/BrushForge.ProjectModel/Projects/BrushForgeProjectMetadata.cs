namespace BrushForge.ProjectModel.Projects;

/// <summary>
/// Immutable project identity and user-facing metadata.
/// </summary>
public sealed record BrushForgeProjectMetadata
{
    public const int MaximumNameLength = 128;

    public BrushForgeProjectMetadata(
        Guid projectId,
        string name,
        DateTimeOffset createdUtc,
        DateTimeOffset modifiedUtc)
    {
        if (projectId == Guid.Empty) {
            throw new ArgumentException(
                "A BrushForge project requires a non-empty identifier.",
                nameof(projectId));
        }

        ValidateUtcTimestamp(
            createdUtc,
            nameof(createdUtc));

        ValidateUtcTimestamp(
            modifiedUtc,
            nameof(modifiedUtc));

        if (modifiedUtc < createdUtc) {
            throw new ArgumentOutOfRangeException(
                nameof(modifiedUtc),
                modifiedUtc,
                "The modified timestamp cannot precede the created timestamp.");
        }

        ProjectId = projectId;
        Name = NormalizeName(name);
        CreatedUtc = createdUtc;
        ModifiedUtc = modifiedUtc;
    }

    public Guid ProjectId { get; }

    public string Name { get; }

    public DateTimeOffset CreatedUtc { get; }

    public DateTimeOffset ModifiedUtc { get; }

    public BrushForgeProjectMetadata Rename(
        string name,
        DateTimeOffset modifiedUtc)
    {
        return new BrushForgeProjectMetadata(
            ProjectId,
            name,
            CreatedUtc,
            modifiedUtc);
    }

    public BrushForgeProjectMetadata Touch(
        DateTimeOffset modifiedUtc)
    {
        return new BrushForgeProjectMetadata(
            ProjectId,
            Name,
            CreatedUtc,
            modifiedUtc);
    }

    private static string NormalizeName(
        string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        string normalizedName =
            name.Trim();

        if (normalizedName.Length == 0) {
            throw new ArgumentException(
                "A BrushForge project requires a name.",
                nameof(name));
        }

        if (
            normalizedName.Length >
            MaximumNameLength
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(name),
                normalizedName.Length,
                $"Project names cannot exceed {MaximumNameLength} characters.");
        }

        if (
            normalizedName.Contains('\r') ||
            normalizedName.Contains('\n')
        ) {
            throw new ArgumentException(
                "Project names cannot contain line breaks.",
                nameof(name));
        }

        return normalizedName;
    }

    private static void ValidateUtcTimestamp(
        DateTimeOffset value,
        string parameterName)
    {
        if (value.Offset != TimeSpan.Zero) {
            throw new ArgumentException(
                "BrushForge project timestamps must use UTC.",
                parameterName);
        }
    }
}
