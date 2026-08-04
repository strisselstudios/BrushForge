namespace BrushForge.ProjectModel.Projects;

/// <summary>
/// Defines the supported BrushForge project-file schema range.
/// </summary>
public static class BrushForgeProjectSchema
{
    public const int MinimumSupportedVersion = 1;
    public const int CurrentVersion = 1;

    public static bool IsSupported(
        int schemaVersion)
    {
        return
            schemaVersion >= MinimumSupportedVersion &&
            schemaVersion <= CurrentVersion;
    }

    public static void EnsureSupported(
        int schemaVersion)
    {
        if (IsSupported(schemaVersion)) {
            return;
        }

        throw new InvalidDataException(
            $"BrushForge project schema version {schemaVersion} is not supported. " +
            $"Supported versions are {MinimumSupportedVersion} through {CurrentVersion}.");
    }
}
