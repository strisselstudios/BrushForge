namespace BrushForge.ProjectModel.Storage;

/// <summary>
/// Defines the public filename conventions for BrushForge project files.
/// </summary>
public static class BrushForgeProjectFileFormat
{
    public const string FileExtension = ".brushforge";

    public static bool HasSupportedExtension(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return string.Equals(
            Path.GetExtension(path),
            FileExtension,
            StringComparison.OrdinalIgnoreCase);
    }

    public static string EnsureExtension(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (HasSupportedExtension(path)) {
            return path;
        }

        return Path.ChangeExtension(
            path,
            FileExtension) ??
            throw new InvalidOperationException(
                "The project path could not be assigned an extension.");
    }
}
