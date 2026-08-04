using BrushForge.Core.Randomness;

namespace BrushForge.ProjectModel.Projects;

/// <summary>
/// Immutable root state for a BrushForge project.
/// </summary>
public sealed record BrushForgeProject
{
    public BrushForgeProject(
        int schemaVersion,
        BrushForgeProjectMetadata metadata,
        BrushForgeProjectSettings settings)
    {
        BrushForgeProjectSchema.EnsureSupported(
            schemaVersion);

        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(settings);

        SchemaVersion = schemaVersion;
        Metadata = metadata;
        Settings = settings;
    }

    public int SchemaVersion { get; }

    public BrushForgeProjectMetadata Metadata { get; }

    public BrushForgeProjectSettings Settings { get; }

    public static BrushForgeProject CreateNew(
        Guid projectId,
        string name,
        DateTimeOffset createdUtc,
        GenerationSeed generationSeed)
    {
        BrushForgeProjectMetadata metadata =
            new(
                projectId,
                name,
                createdUtc,
                createdUtc);

        BrushForgeProjectSettings settings =
            BrushForgeProjectSettings.CreateDefault(
                generationSeed);

        return new BrushForgeProject(
            BrushForgeProjectSchema.CurrentVersion,
            metadata,
            settings);
    }

    public BrushForgeProject Rename(
        string name,
        DateTimeOffset modifiedUtc)
    {
        return new BrushForgeProject(
            SchemaVersion,
            Metadata.Rename(
                name,
                modifiedUtc),
            Settings);
    }

    public BrushForgeProject WithSettings(
        BrushForgeProjectSettings settings,
        DateTimeOffset modifiedUtc)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new BrushForgeProject(
            SchemaVersion,
            Metadata.Touch(
                modifiedUtc),
            settings);
    }

    public BrushForgeProject WithActiveWorkspace(
        BrushForgeWorkspaceKind activeWorkspace,
        DateTimeOffset modifiedUtc)
    {
        return WithSettings(
            Settings.WithActiveWorkspace(
                activeWorkspace),
            modifiedUtc);
    }
}
