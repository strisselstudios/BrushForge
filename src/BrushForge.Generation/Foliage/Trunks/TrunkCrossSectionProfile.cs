namespace BrushForge.Generation.Foliage;

/// <summary>
/// Supported convex cross-sections for generated tree trunks.
/// </summary>
public enum TrunkCrossSectionProfile
{
    /// <summary>
    /// Four vertical side faces.
    /// </summary>
    Square = 0,

    /// <summary>
    /// Eight vertical side faces for a rounder faceted silhouette.
    /// </summary>
    Octagonal = 1
}
