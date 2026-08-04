using System.Globalization;
using System.Text.Json;
using BrushForge.Core.Coordinates;
using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.ProjectModel.Projects;

namespace BrushForge.ProjectModel.Serialization;

/// <summary>
/// Parses strict BrushForge project JSON into the immutable project model.
/// </summary>
internal static class BrushForgeProjectJsonReader
{
    private static readonly string[] RootProperties =
    [
        "schemaVersion",
        "metadata",
        "settings"
    ];

    private static readonly string[] MetadataProperties =
    [
        "projectId",
        "name",
        "createdUtc",
        "modifiedUtc"
    ];

    private static readonly string[] SettingsProperties =
    [
        "activeWorkspace",
        "generationSeed",
        "gridSpacing",
        "mapUnitsPerSourceUnit"
    ];

    public static BrushForgeProject Read(
        string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        try {
            JsonDocumentOptions options = new()
            {
                AllowTrailingCommas = false,
                CommentHandling =
                    JsonCommentHandling.Disallow,
                MaxDepth = 16
            };

            using JsonDocument document =
                JsonDocument.Parse(
                    json,
                    options);

            return ParseProject(
                document.RootElement);
        }
        catch (JsonException exception) {
            throw CreateInvalidDataException(
                "The project text is not valid JSON.",
                exception);
        }
        catch (FormatException exception) {
            throw CreateInvalidDataException(
                "The project JSON contains a value with an invalid format.",
                exception);
        }
        catch (OverflowException exception) {
            throw CreateInvalidDataException(
                "The project JSON contains a numeric value outside its supported range.",
                exception);
        }
        catch (ArgumentException exception) {
            throw CreateInvalidDataException(
                "The project JSON contains invalid BrushForge project data.",
                exception);
        }
        catch (InvalidOperationException exception) {
            throw CreateInvalidDataException(
                "The project JSON contains a value with the wrong JSON type.",
                exception);
        }
    }

    private static BrushForgeProject ParseProject(
        JsonElement root)
    {
        ValidateObject(
            root,
            "$",
            RootProperties);

        int schemaVersion =
            ReadRequiredInt32(
                root,
                "schemaVersion",
                "$.schemaVersion");

        BrushForgeProjectSchema.EnsureSupported(
            schemaVersion);

        BrushForgeProjectMetadata metadata =
            ParseMetadata(
                ReadRequiredObject(
                    root,
                    "metadata",
                    "$.metadata"));

        BrushForgeProjectSettings settings =
            ParseSettings(
                ReadRequiredObject(
                    root,
                    "settings",
                    "$.settings"));

        return new BrushForgeProject(
            schemaVersion,
            metadata,
            settings);
    }

    private static BrushForgeProjectMetadata ParseMetadata(
        JsonElement metadata)
    {
        ValidateObject(
            metadata,
            "$.metadata",
            MetadataProperties);

        Guid projectId =
            ParseProjectId(
                ReadRequiredString(
                    metadata,
                    "projectId",
                    "$.metadata.projectId"));

        string name =
            ReadRequiredString(
                metadata,
                "name",
                "$.metadata.name");

        DateTimeOffset createdUtc =
            ParseTimestamp(
                ReadRequiredString(
                    metadata,
                    "createdUtc",
                    "$.metadata.createdUtc"),
                "$.metadata.createdUtc");

        DateTimeOffset modifiedUtc =
            ParseTimestamp(
                ReadRequiredString(
                    metadata,
                    "modifiedUtc",
                    "$.metadata.modifiedUtc"),
                "$.metadata.modifiedUtc");

        return new BrushForgeProjectMetadata(
            projectId,
            name,
            createdUtc,
            modifiedUtc);
    }

    private static BrushForgeProjectSettings ParseSettings(
        JsonElement settings)
    {
        ValidateObject(
            settings,
            "$.settings",
            SettingsProperties);

        BrushForgeWorkspaceKind workspace =
            ParseWorkspace(
                ReadRequiredString(
                    settings,
                    "activeWorkspace",
                    "$.settings.activeWorkspace"));

        GenerationSeed generationSeed =
            ParseGenerationSeed(
                ReadRequiredString(
                    settings,
                    "generationSeed",
                    "$.settings.generationSeed"));

        double gridSpacing =
            ReadRequiredDouble(
                settings,
                "gridSpacing",
                "$.settings.gridSpacing");

        double coordinateScale =
            ReadRequiredDouble(
                settings,
                "mapUnitsPerSourceUnit",
                "$.settings.mapUnitsPerSourceUnit");

        return new BrushForgeProjectSettings(
            workspace,
            generationSeed,
            new GridSpacing(
                gridSpacing),
            new CoordinateScale(
                coordinateScale));
    }

    private static void ValidateObject(
        JsonElement element,
        string path,
        string[] allowedProperties)
    {
        if (
            element.ValueKind !=
            JsonValueKind.Object
        ) {
            throw new InvalidDataException(
                $"{path} must be a JSON object.");
        }

        HashSet<string> encounteredProperties =
            new(
                StringComparer.Ordinal);

        foreach (
            JsonProperty property
            in element.EnumerateObject()
        ) {
            if (
                Array.IndexOf(
                    allowedProperties,
                    property.Name) < 0
            ) {
                throw new InvalidDataException(
                    $"{path} contains unknown property '{property.Name}'.");
            }

            if (
                !encounteredProperties.Add(
                    property.Name)
            ) {
                throw new InvalidDataException(
                    $"{path} contains duplicate property '{property.Name}'.");
            }
        }

        foreach (
            string requiredProperty
            in allowedProperties
        ) {
            if (
                !encounteredProperties.Contains(
                    requiredProperty)
            ) {
                throw new InvalidDataException(
                    $"{path} is missing required property '{requiredProperty}'.");
            }
        }
    }

    private static JsonElement ReadRequiredObject(
        JsonElement parent,
        string propertyName,
        string path)
    {
        JsonElement value =
            parent.GetProperty(
                propertyName);

        if (
            value.ValueKind !=
            JsonValueKind.Object
        ) {
            throw new InvalidDataException(
                $"{path} must be a JSON object.");
        }

        return value;
    }

    private static string ReadRequiredString(
        JsonElement parent,
        string propertyName,
        string path)
    {
        JsonElement value =
            parent.GetProperty(
                propertyName);

        if (
            value.ValueKind !=
            JsonValueKind.String
        ) {
            throw new InvalidDataException(
                $"{path} must be a JSON string.");
        }

        return value.GetString() ??
            throw new InvalidDataException(
                $"{path} cannot be null.");
    }

    private static int ReadRequiredInt32(
        JsonElement parent,
        string propertyName,
        string path)
    {
        JsonElement value =
            parent.GetProperty(
                propertyName);

        if (
            value.ValueKind !=
                JsonValueKind.Number ||
            !value.TryGetInt32(
                out int result)
        ) {
            throw new InvalidDataException(
                $"{path} must be a 32-bit JSON integer.");
        }

        return result;
    }

    private static double ReadRequiredDouble(
        JsonElement parent,
        string propertyName,
        string path)
    {
        JsonElement value =
            parent.GetProperty(
                propertyName);

        if (
            value.ValueKind !=
                JsonValueKind.Number ||
            !value.TryGetDouble(
                out double result)
        ) {
            throw new InvalidDataException(
                $"{path} must be a JSON number.");
        }

        return result;
    }

    private static Guid ParseProjectId(
        string value)
    {
        if (
            !Guid.TryParseExact(
                value,
                "D",
                out Guid projectId)
        ) {
            throw new InvalidDataException(
                "$.metadata.projectId must use Guid D format.");
        }

        return projectId;
    }

    private static DateTimeOffset ParseTimestamp(
        string value,
        string path)
    {
        if (
            !DateTimeOffset.TryParseExact(
                value,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTimeOffset timestamp)
        ) {
            throw new InvalidDataException(
                $"{path} must use the round-trip timestamp format.");
        }

        return timestamp;
    }

    private static BrushForgeWorkspaceKind ParseWorkspace(
        string value)
    {
        return value switch
        {
            "foliage" =>
                BrushForgeWorkspaceKind.Foliage,

            "terrain" =>
                BrushForgeWorkspaceKind.Terrain,

            _ =>
                throw new InvalidDataException(
                    $"$.settings.activeWorkspace value '{value}' is not recognized.")
        };
    }

    private static GenerationSeed ParseGenerationSeed(
        string value)
    {
        if (
            !GenerationSeed.TryParse(
                value,
                out GenerationSeed seed)
        ) {
            throw new InvalidDataException(
                "$.settings.generationSeed must be an unsigned 64-bit decimal string.");
        }

        return seed;
    }

    private static InvalidDataException CreateInvalidDataException(
        string message,
        Exception innerException)
    {
        return new InvalidDataException(
            message,
            innerException);
    }
}
