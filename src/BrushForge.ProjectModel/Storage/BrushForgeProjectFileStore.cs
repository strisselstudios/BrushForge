using System.Text;
using BrushForge.ProjectModel.Projects;
using BrushForge.ProjectModel.Serialization;

namespace BrushForge.ProjectModel.Storage;

/// <summary>
/// Loads and saves canonical BrushForge project files.
/// </summary>
public static class BrushForgeProjectFileStore
{
    private static readonly UTF8Encoding StrictUtf8WithoutBom =
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    public static BrushForgeProject Load(
        string path)
    {
        string fullPath =
            GetFullPath(path);

        try {
            string json =
                File.ReadAllText(
                    fullPath,
                    StrictUtf8WithoutBom);

            return BrushForgeProjectJsonSerializer.Deserialize(
                json);
        }
        catch (DecoderFallbackException exception) {
            throw new InvalidDataException(
                $"BrushForge project file '{fullPath}' is not valid UTF-8.",
                exception);
        }
    }

    public static void Save(
        string path,
        BrushForgeProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        string fullPath =
            GetFullPath(path);

        string json =
            BrushForgeProjectJsonSerializer.Serialize(
                project);

        string directoryPath =
            Path.GetDirectoryName(fullPath) ??
            throw new InvalidOperationException(
                "The project path does not have a parent directory.");

        Directory.CreateDirectory(
            directoryPath);

        string temporaryPath =
            CreateTemporaryPath(
                directoryPath,
                Path.GetFileName(fullPath));

        try {
            File.WriteAllText(
                temporaryPath,
                json,
                StrictUtf8WithoutBom);

            File.Move(
                temporaryPath,
                fullPath,
                overwrite: true);
        }
        finally {
            File.Delete(
                temporaryPath);
        }
    }

    private static string GetFullPath(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Path.GetFullPath(path);
    }

    private static string CreateTemporaryPath(
        string directoryPath,
        string fileName)
    {
        return Path.Combine(
            directoryPath,
            $".{fileName}.{Guid.NewGuid():N}.tmp");
    }
}
