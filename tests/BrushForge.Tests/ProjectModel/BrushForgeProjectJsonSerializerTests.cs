using BrushForge.Core.Coordinates;
using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.ProjectModel.Projects;
using BrushForge.ProjectModel.Serialization;

namespace BrushForge.Tests.ProjectModel;

public sealed class BrushForgeProjectJsonSerializerTests
{
    private static readonly DateTimeOffset CreatedUtc =
        new(
            2026,
            8,
            3,
            12,
            0,
            0,
            TimeSpan.Zero);

    private static readonly DateTimeOffset ModifiedUtc =
        CreatedUtc.AddHours(2.0);

    [Fact]
    public void SerializeWritesCanonicalIndentedJson()
    {
        BrushForgeProject project =
            CreateProject(
                name:
                    "Foliage \"Study\"",
                workspace:
                    BrushForgeWorkspaceKind.Terrain,
                seed:
                    ulong.MaxValue,
                gridSpacing:
                    16.0,
                coordinateScale:
                    2.5);

        string actual =
            BrushForgeProjectJsonSerializer.Serialize(
                project);

        const string expected =
            "{\n" +
            "  \"schemaVersion\": 1,\n" +
            "  \"metadata\": {\n" +
            "    \"projectId\": \"5039f3e7-9f0a-49d4-bbe4-89d85d09c01e\",\n" +
            "    \"name\": \"Foliage \\\"Study\\\"\",\n" +
            "    \"createdUtc\": \"2026-08-03T12:00:00.0000000+00:00\",\n" +
            "    \"modifiedUtc\": \"2026-08-03T14:00:00.0000000+00:00\"\n" +
            "  },\n" +
            "  \"settings\": {\n" +
            "    \"activeWorkspace\": \"terrain\",\n" +
            "    \"generationSeed\": \"18446744073709551615\",\n" +
            "    \"gridSpacing\": 16,\n" +
            "    \"mapUnitsPerSourceUnit\": 2.5\n" +
            "  }\n" +
            "}\n";

        Assert.Equal(
            expected,
            actual);
    }

    [Fact]
    public void SerializeCanWriteCompactJson()
    {
        BrushForgeProject project =
            CreateProject();

        string actual =
            BrushForgeProjectJsonSerializer.Serialize(
                project,
                indented: false);

        const string expected =
            "{\"schemaVersion\":1," +
            "\"metadata\":{" +
            "\"projectId\":\"5039f3e7-9f0a-49d4-bbe4-89d85d09c01e\"," +
            "\"name\":\"Project\"," +
            "\"createdUtc\":\"2026-08-03T12:00:00.0000000+00:00\"," +
            "\"modifiedUtc\":\"2026-08-03T14:00:00.0000000+00:00\"}," +
            "\"settings\":{" +
            "\"activeWorkspace\":\"foliage\"," +
            "\"generationSeed\":\"1\"," +
            "\"gridSpacing\":8," +
            "\"mapUnitsPerSourceUnit\":1}}\n";

        Assert.Equal(
            expected,
            actual);
    }

    [Fact]
    public void DeserializeReconstructsProject()
    {
        BrushForgeProject source =
            CreateProject(
                name:
                    "Terrain Study",
                workspace:
                    BrushForgeWorkspaceKind.Terrain,
                seed:
                    987654321UL,
                gridSpacing:
                    32.0,
                coordinateScale:
                    0.5);

        string json =
            BrushForgeProjectJsonSerializer.Serialize(
                source);

        BrushForgeProject parsed =
            BrushForgeProjectJsonSerializer.Deserialize(
                json);

        Assert.Equal(
            source,
            parsed);
    }

    [Fact]
    public void RoundTripProducesCanonicalJson()
    {
        string first =
            BrushForgeProjectJsonSerializer.Serialize(
                CreateProject());

        BrushForgeProject parsed =
            BrushForgeProjectJsonSerializer.Deserialize(
                first);

        string second =
            BrushForgeProjectJsonSerializer.Serialize(
                parsed);

        Assert.Equal(
            first,
            second);
    }

    [Fact]
    public void DeserializeRejectsMalformedJson()
    {
        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    "{]"));
    }

    [Fact]
    public void DeserializeRejectsNonObjectRoot()
    {
        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    "[]"));
    }

    [Fact]
    public void DeserializeRejectsMissingTopLevelProperty()
    {
        string json =
            SerializeDefaultProject()
                .Replace(
                    "  \"schemaVersion\": 1,\n",
                    string.Empty,
                    StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void DeserializeRejectsUnknownTopLevelProperty()
    {
        string json =
            SerializeDefaultProject()
                .Insert(
                    2,
                    "  \"unexpected\": true,\n");

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void DeserializeRejectsDuplicateTopLevelProperty()
    {
        string json =
            SerializeDefaultProject()
                .Insert(
                    2,
                    "  \"schemaVersion\": 1,\n");

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void DeserializeRejectsUnsupportedSchemaVersion()
    {
        string json =
            SerializeDefaultProject()
                .Replace(
                    "\"schemaVersion\": 1",
                    "\"schemaVersion\": 2",
                    StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void DeserializeRejectsInvalidProjectIdentifier()
    {
        string json =
            SerializeDefaultProject()
                .Replace(
                    "5039f3e7-9f0a-49d4-bbe4-89d85d09c01e",
                    "not-a-guid",
                    StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void DeserializeRejectsNonUtcTimestamp()
    {
        string json =
            SerializeDefaultProject()
                .Replace(
                    "2026-08-03T12:00:00.0000000+00:00",
                    "2026-08-03T12:00:00.0000000-05:00",
                    StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void DeserializeRejectsUnknownWorkspace()
    {
        string json =
            SerializeDefaultProject()
                .Replace(
                    "\"activeWorkspace\": \"foliage\"",
                    "\"activeWorkspace\": \"unknown\"",
                    StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void DeserializeRejectsInvalidGenerationSeed()
    {
        string json =
            SerializeDefaultProject()
                .Replace(
                    "\"generationSeed\": \"1\"",
                    "\"generationSeed\": \"-1\"",
                    StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    json));
    }

    [Fact]
    public void DeserializeRejectsInvalidNumericSettings()
    {
        string invalidGrid =
            SerializeDefaultProject()
                .Replace(
                    "\"gridSpacing\": 8",
                    "\"gridSpacing\": 0",
                    StringComparison.Ordinal);

        string invalidScale =
            SerializeDefaultProject()
                .Replace(
                    "\"mapUnitsPerSourceUnit\": 1",
                    "\"mapUnitsPerSourceUnit\": -1",
                    StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    invalidGrid));

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    invalidScale));
    }

    [Fact]
    public void DeserializeRejectsUnknownNestedProperty()
    {
        string json =
            SerializeDefaultProject()
                .Replace(
                    "    \"name\": \"Project\",\n",
                    "    \"name\": \"Project\",\n    \"unexpected\": true,\n",
                    StringComparison.Ordinal);

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectJsonSerializer.Deserialize(
                    json));
    }

    private static string SerializeDefaultProject()
    {
        return BrushForgeProjectJsonSerializer.Serialize(
            CreateProject());
    }

    private static BrushForgeProject CreateProject(
        string name = "Project",
        BrushForgeWorkspaceKind workspace =
            BrushForgeWorkspaceKind.Foliage,
        ulong seed = 1UL,
        double gridSpacing = 8.0,
        double coordinateScale = 1.0)
    {
        BrushForgeProjectMetadata metadata =
            new(
                Guid.Parse(
                    "5039f3e7-9f0a-49d4-bbe4-89d85d09c01e"),
                name,
                CreatedUtc,
                ModifiedUtc);

        BrushForgeProjectSettings settings =
            new(
                workspace,
                new GenerationSeed(
                    seed),
                new GridSpacing(
                    gridSpacing),
                new CoordinateScale(
                    coordinateScale));

        return new BrushForgeProject(
            BrushForgeProjectSchema.CurrentVersion,
            metadata,
            settings);
    }
}
