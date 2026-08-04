using BrushForge.Core.Coordinates;
using BrushForge.Core.Grid;
using BrushForge.Core.Randomness;

namespace BrushForge.ProjectModel.Projects;

/// <summary>
/// Shared project settings used by deterministic generators and workspaces.
/// </summary>
public sealed record BrushForgeProjectSettings
{
    public BrushForgeProjectSettings(
        BrushForgeWorkspaceKind activeWorkspace,
        GenerationSeed generationSeed,
        GridSpacing gridSpacing,
        CoordinateScale coordinateScale)
    {
        if (!Enum.IsDefined(activeWorkspace)) {
            throw new ArgumentOutOfRangeException(
                nameof(activeWorkspace),
                activeWorkspace,
                "The active workspace is not recognized.");
        }

        ArgumentNullException.ThrowIfNull(gridSpacing);
        ArgumentNullException.ThrowIfNull(coordinateScale);

        ActiveWorkspace = activeWorkspace;
        GenerationSeed = generationSeed;
        GridSpacing = gridSpacing;
        CoordinateScale = coordinateScale;
    }

    public BrushForgeWorkspaceKind ActiveWorkspace { get; }

    public GenerationSeed GenerationSeed { get; }

    public GridSpacing GridSpacing { get; }

    public CoordinateScale CoordinateScale { get; }

    public static BrushForgeProjectSettings CreateDefault(
        GenerationSeed generationSeed)
    {
        return new BrushForgeProjectSettings(
            BrushForgeWorkspaceKind.Foliage,
            generationSeed,
            GridSpacing.Eight,
            CoordinateScale.Identity);
    }

    public BrushForgeProjectSettings WithActiveWorkspace(
        BrushForgeWorkspaceKind activeWorkspace)
    {
        return new BrushForgeProjectSettings(
            activeWorkspace,
            GenerationSeed,
            GridSpacing,
            CoordinateScale);
    }

    public BrushForgeProjectSettings WithGenerationSeed(
        GenerationSeed generationSeed)
    {
        return new BrushForgeProjectSettings(
            ActiveWorkspace,
            generationSeed,
            GridSpacing,
            CoordinateScale);
    }

    public BrushForgeProjectSettings WithGridSpacing(
        GridSpacing gridSpacing)
    {
        return new BrushForgeProjectSettings(
            ActiveWorkspace,
            GenerationSeed,
            gridSpacing,
            CoordinateScale);
    }

    public BrushForgeProjectSettings WithCoordinateScale(
        CoordinateScale coordinateScale)
    {
        return new BrushForgeProjectSettings(
            ActiveWorkspace,
            GenerationSeed,
            GridSpacing,
            coordinateScale);
    }
}
