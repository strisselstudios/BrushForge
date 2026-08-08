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
    string CanopyTextureName,
    string TrunkSegmentCount = "3",
    string TrunkTaperPercent = "25",
    string TrunkLeanPercent = "0",
    string TrunkBendPercent = "0",
    string TrunkBaseFlarePercent = "0",
    string TrunkCrossSection = "Octagonal",
    string TrunkIrregularityPercent = "0",
    string TrunkTwistPercent = "0",
    string DetailPercent = "100");
