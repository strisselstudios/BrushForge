namespace BrushForge.Core.Coordinates;

public enum CoordinateAxis
{
    X,
    Y,
    Z
}

public enum CoordinateHandedness
{
    LeftHanded,
    RightHanded
}

/// <summary>
/// Defines the permanent internal coordinate convention used by BrushForge.
/// </summary>
public static class BrushForgeCoordinateSystem
{
    public const CoordinateAxis RightAxis = CoordinateAxis.X;
    public const CoordinateAxis ForwardAxis = CoordinateAxis.Y;
    public const CoordinateAxis UpAxis = CoordinateAxis.Z;
    public const CoordinateHandedness Handedness = CoordinateHandedness.RightHanded;

    /// <summary>
    /// One internal BrushForge unit equals one exported map unit unless an
    /// explicit source-to-map scale is applied.
    /// </summary>
    public const double MapUnitsPerInternalUnit = 1.0;
}
