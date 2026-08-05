namespace BrushForge.Generation.Foliage.Input;

/// <summary>
/// Text values supplied by a user interface before tree-generation parsing.
/// </summary>
public sealed record TreeGenerationInput(
    string GenerationSeed,
    string OverallHeight,
    string TrunkWidth,
    string CanopyWidth,
    string CanopyHeight,
    string CanopyLayerCount,
    string GridSpacing,
    string TrunkTextureName,
    string CanopyTextureName);
