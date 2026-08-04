using BrushForge.Core.Coordinates;
using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;
using BrushForge.ProjectModel.Projects;

namespace BrushForge.Tests.ProjectModel;

public sealed class BrushForgeProjectTests
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
    public void CurrentSchemaVersionIsSupported()
    {
        Assert.True(
            BrushForgeProjectSchema.IsSupported(
                BrushForgeProjectSchema.CurrentVersion));
    }

    [Fact]
    public void UnsupportedSchemaVersionsAreRejected()
    {
        Assert.False(
            BrushForgeProjectSchema.IsSupported(0));

        Assert.False(
            BrushForgeProjectSchema.IsSupported(
                BrushForgeProjectSchema.CurrentVersion + 1));

        Assert.Throws<InvalidDataException>(
            () =>
                BrushForgeProjectSchema.EnsureSupported(0));
    }

    [Fact]
    public void MetadataNormalizesNameAndPreservesIdentity()
    {
        Guid projectId =
            Guid.Parse(
                "5039f3e7-9f0a-49d4-bbe4-89d85d09c01e");

        BrushForgeProjectMetadata metadata =
            new(
                projectId,
                "  Tutorial World  ",
                CreatedUtc,
                ModifiedUtc);

        Assert.Equal(
            projectId,
            metadata.ProjectId);

        Assert.Equal(
            "Tutorial World",
            metadata.Name);

        Assert.Equal(
            CreatedUtc,
            metadata.CreatedUtc);

        Assert.Equal(
            ModifiedUtc,
            metadata.ModifiedUtc);
    }

    [Fact]
    public void MetadataRejectsEmptyIdentifier()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new BrushForgeProjectMetadata(
                    Guid.Empty,
                    "Project",
                    CreatedUtc,
                    ModifiedUtc));
    }

    [Fact]
    public void MetadataRejectsInvalidNames()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new BrushForgeProjectMetadata(
                    Guid.NewGuid(),
                    "   ",
                    CreatedUtc,
                    ModifiedUtc));

        Assert.Throws<ArgumentException>(
            () =>
                new BrushForgeProjectMetadata(
                    Guid.NewGuid(),
                    "First\nSecond",
                    CreatedUtc,
                    ModifiedUtc));

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BrushForgeProjectMetadata(
                    Guid.NewGuid(),
                    new string(
                        'A',
                        BrushForgeProjectMetadata.MaximumNameLength + 1),
                    CreatedUtc,
                    ModifiedUtc));
    }

    [Fact]
    public void MetadataRequiresUtcTimestamps()
    {
        DateTimeOffset nonUtc =
            new(
                2026,
                8,
                3,
                12,
                0,
                0,
                TimeSpan.FromHours(-5.0));

        Assert.Throws<ArgumentException>(
            () =>
                new BrushForgeProjectMetadata(
                    Guid.NewGuid(),
                    "Project",
                    nonUtc,
                    ModifiedUtc));

        Assert.Throws<ArgumentException>(
            () =>
                new BrushForgeProjectMetadata(
                    Guid.NewGuid(),
                    "Project",
                    CreatedUtc,
                    nonUtc));
    }

    [Fact]
    public void MetadataRejectsModifiedTimeBeforeCreatedTime()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BrushForgeProjectMetadata(
                    Guid.NewGuid(),
                    "Project",
                    CreatedUtc,
                    CreatedUtc.AddMinutes(-1.0)));
    }

    [Fact]
    public void RenameReturnsNewMetadataWithoutChangingIdentity()
    {
        Guid projectId =
            Guid.Parse(
                "2a68d7bf-5eaf-4c11-bddc-42582a98efb1");

        BrushForgeProjectMetadata source =
            new(
                projectId,
                "Before",
                CreatedUtc,
                CreatedUtc);

        BrushForgeProjectMetadata renamed =
            source.Rename(
                "After",
                ModifiedUtc);

        Assert.Equal(
            "Before",
            source.Name);

        Assert.Equal(
            "After",
            renamed.Name);

        Assert.Equal(
            projectId,
            renamed.ProjectId);

        Assert.Equal(
            CreatedUtc,
            renamed.CreatedUtc);

        Assert.Equal(
            ModifiedUtc,
            renamed.ModifiedUtc);
    }

    [Fact]
    public void DefaultSettingsUseFoliageIdentityScaleAndEightUnitGrid()
    {
        GenerationSeed seed =
            new(1234UL);

        BrushForgeProjectSettings settings =
            BrushForgeProjectSettings.CreateDefault(
                seed);

        Assert.Equal(
            BrushForgeWorkspaceKind.Foliage,
            settings.ActiveWorkspace);

        Assert.Equal(
            seed,
            settings.GenerationSeed);

        Assert.Equal(
            GridSpacing.Eight,
            settings.GridSpacing);

        Assert.Equal(
            CoordinateScale.Identity,
            settings.CoordinateScale);
    }

    [Fact]
    public void SettingsRejectUndefinedWorkspace()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new BrushForgeProjectSettings(
                    (BrushForgeWorkspaceKind)999,
                    new GenerationSeed(1UL),
                    GridSpacing.Eight,
                    CoordinateScale.Identity));
    }

    [Fact]
    public void SettingsEditsPreserveUnchangedValues()
    {
        BrushForgeProjectSettings source =
            BrushForgeProjectSettings.CreateDefault(
                new GenerationSeed(1UL));

        GridSpacing replacementGrid =
            GridSpacing.Sixteen;

        CoordinateScale replacementScale =
            new(2.0);

        BrushForgeProjectSettings edited =
            source
                .WithActiveWorkspace(
                    BrushForgeWorkspaceKind.Terrain)
                .WithGenerationSeed(
                    new GenerationSeed(2UL))
                .WithGridSpacing(
                    replacementGrid)
                .WithCoordinateScale(
                    replacementScale);

        Assert.Equal(
            BrushForgeWorkspaceKind.Foliage,
            source.ActiveWorkspace);

        Assert.Equal(
            BrushForgeWorkspaceKind.Terrain,
            edited.ActiveWorkspace);

        Assert.Equal(
            new GenerationSeed(2UL),
            edited.GenerationSeed);

        Assert.Same(
            replacementGrid,
            edited.GridSpacing);

        Assert.Same(
            replacementScale,
            edited.CoordinateScale);
    }

    [Fact]
    public void CreateNewUsesCurrentSchemaAndDeterministicDefaults()
    {
        Guid projectId =
            Guid.Parse(
                "75bb276d-7f31-40e8-85ac-34582f80620d");

        GenerationSeed seed =
            new(987654321UL);

        BrushForgeProject project =
            BrushForgeProject.CreateNew(
                projectId,
                "Foliage Study",
                CreatedUtc,
                seed);

        Assert.Equal(
            BrushForgeProjectSchema.CurrentVersion,
            project.SchemaVersion);

        Assert.Equal(
            projectId,
            project.Metadata.ProjectId);

        Assert.Equal(
            CreatedUtc,
            project.Metadata.CreatedUtc);

        Assert.Equal(
            CreatedUtc,
            project.Metadata.ModifiedUtc);

        Assert.Equal(
            seed,
            project.Settings.GenerationSeed);

        Assert.Equal(
            BrushForgeWorkspaceKind.Foliage,
            project.Settings.ActiveWorkspace);
    }

    [Fact]
    public void ProjectRejectsUnsupportedSchema()
    {
        BrushForgeProjectMetadata metadata =
            new(
                Guid.NewGuid(),
                "Project",
                CreatedUtc,
                CreatedUtc);

        BrushForgeProjectSettings settings =
            BrushForgeProjectSettings.CreateDefault(
                new GenerationSeed(1UL));

        Assert.Throws<InvalidDataException>(
            () =>
                new BrushForgeProject(
                    0,
                    metadata,
                    settings));
    }

    [Fact]
    public void RenameReturnsNewProjectAndTouchesMetadata()
    {
        BrushForgeProject source =
            CreateProject();

        BrushForgeProject renamed =
            source.Rename(
                "Renamed",
                ModifiedUtc);

        Assert.Equal(
            "Project",
            source.Metadata.Name);

        Assert.Equal(
            "Renamed",
            renamed.Metadata.Name);

        Assert.Equal(
            ModifiedUtc,
            renamed.Metadata.ModifiedUtc);

        Assert.Same(
            source.Settings,
            renamed.Settings);
    }

    [Fact]
    public void WithSettingsTouchesMetadataAndPreservesSource()
    {
        BrushForgeProject source =
            CreateProject();

        BrushForgeProjectSettings settings =
            source.Settings.WithGenerationSeed(
                new GenerationSeed(77UL));

        BrushForgeProject edited =
            source.WithSettings(
                settings,
                ModifiedUtc);

        Assert.Equal(
            new GenerationSeed(1UL),
            source.Settings.GenerationSeed);

        Assert.Equal(
            new GenerationSeed(77UL),
            edited.Settings.GenerationSeed);

        Assert.Equal(
            ModifiedUtc,
            edited.Metadata.ModifiedUtc);

        Assert.Equal(
            source.Metadata.ProjectId,
            edited.Metadata.ProjectId);
    }

    [Fact]
    public void WithActiveWorkspaceUpdatesWorkspaceAndTimestamp()
    {
        BrushForgeProject source =
            CreateProject();

        BrushForgeProject edited =
            source.WithActiveWorkspace(
                BrushForgeWorkspaceKind.Terrain,
                ModifiedUtc);

        Assert.Equal(
            BrushForgeWorkspaceKind.Foliage,
            source.Settings.ActiveWorkspace);

        Assert.Equal(
            BrushForgeWorkspaceKind.Terrain,
            edited.Settings.ActiveWorkspace);

        Assert.Equal(
            ModifiedUtc,
            edited.Metadata.ModifiedUtc);
    }

    private static BrushForgeProject CreateProject()
    {
        return BrushForgeProject.CreateNew(
            Guid.Parse(
                "6a2c2487-fefe-4b69-96dc-f29f9fac25db"),
            "Project",
            CreatedUtc,
            new GenerationSeed(1UL));
    }
}
