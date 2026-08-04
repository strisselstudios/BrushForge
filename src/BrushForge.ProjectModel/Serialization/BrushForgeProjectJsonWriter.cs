using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BrushForge.ProjectModel.Projects;

namespace BrushForge.ProjectModel.Serialization;

/// <summary>
/// Writes canonical BrushForge project JSON.
/// </summary>
internal static class BrushForgeProjectJsonWriter
{
    public static string Write(
        BrushForgeProject project,
        bool indented)
    {
        ArgumentNullException.ThrowIfNull(project);

        using MemoryStream stream = new();

        JsonWriterOptions options = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = indented,
            SkipValidation = false
        };

        using (
            Utf8JsonWriter writer =
                new(
                    stream,
                    options)
        ) {
            WriteProject(
                writer,
                project);

            writer.Flush();
        }

        string json =
            Encoding.UTF8.GetString(
                stream.ToArray());

        string normalizedJson =
            json.Replace(
                "\r\n",
                "\n",
                StringComparison.Ordinal).Replace(
                    "\r",
                    "\n",
                    StringComparison.Ordinal);

        return normalizedJson + "\n";
    }

    private static void WriteProject(
        Utf8JsonWriter writer,
        BrushForgeProject project)
    {
        writer.WriteStartObject();

        writer.WriteNumber(
            "schemaVersion",
            project.SchemaVersion);

        WriteMetadata(
            writer,
            project.Metadata);

        WriteSettings(
            writer,
            project.Settings);

        writer.WriteEndObject();
    }

    private static void WriteMetadata(
        Utf8JsonWriter writer,
        BrushForgeProjectMetadata metadata)
    {
        writer.WritePropertyName(
            "metadata");

        writer.WriteStartObject();

        writer.WriteString(
            "projectId",
            metadata.ProjectId.ToString(
                "D",
                CultureInfo.InvariantCulture));

        writer.WriteString(
            "name",
            metadata.Name);

        writer.WriteString(
            "createdUtc",
            metadata.CreatedUtc.ToString(
                "O",
                CultureInfo.InvariantCulture));

        writer.WriteString(
            "modifiedUtc",
            metadata.ModifiedUtc.ToString(
                "O",
                CultureInfo.InvariantCulture));

        writer.WriteEndObject();
    }

    private static void WriteSettings(
        Utf8JsonWriter writer,
        BrushForgeProjectSettings settings)
    {
        writer.WritePropertyName(
            "settings");

        writer.WriteStartObject();

        writer.WriteString(
            "activeWorkspace",
            FormatWorkspace(
                settings.ActiveWorkspace));

        writer.WriteString(
            "generationSeed",
            settings.GenerationSeed.ToString());

        writer.WriteNumber(
            "gridSpacing",
            settings.GridSpacing.Units);

        writer.WriteNumber(
            "mapUnitsPerSourceUnit",
            settings.CoordinateScale.MapUnitsPerSourceUnit);

        writer.WriteEndObject();
    }

    private static string FormatWorkspace(
        BrushForgeWorkspaceKind workspace)
    {
        return workspace switch
        {
            BrushForgeWorkspaceKind.Foliage =>
                "foliage",

            BrushForgeWorkspaceKind.Terrain =>
                "terrain",

            _ =>
                throw new InvalidOperationException(
                    $"Workspace value {workspace} cannot be serialized.")
        };
    }
}
