using BrushForge.Core.Numerics;

namespace BrushForge.Geometry.Validation;

/// <summary>
/// Numeric policy used when reconstructing and validating a convex brush.
/// </summary>
public sealed record BrushValidationSettings
{
    public const double DefaultDeterminantTolerance = 0.000000001;
    public const double DefaultHalfSpaceTolerance = 0.00001;
    public const double DefaultPlaneMembershipTolerance = 0.00001;
    public const double DefaultVertexMergeTolerance = 0.00001;
    public const double DefaultInteriorTolerance = 0.0000001;
    public const double DefaultMinimumFaceArea = 0.0001;
    public const double DefaultMinimumExtent = 0.0001;
    public const double DefaultMinimumVolume = 0.000001;

    public static BrushValidationSettings Default { get; } = new();

    public BrushValidationSettings(
        double determinantTolerance = DefaultDeterminantTolerance,
        double halfSpaceTolerance = DefaultHalfSpaceTolerance,
        double planeMembershipTolerance = DefaultPlaneMembershipTolerance,
        double vertexMergeTolerance = DefaultVertexMergeTolerance,
        double interiorTolerance = DefaultInteriorTolerance,
        double minimumFaceArea = DefaultMinimumFaceArea,
        double minimumExtent = DefaultMinimumExtent,
        double minimumVolume = DefaultMinimumVolume)
    {
        DeterminantTolerance = ValidateNonNegative(
            determinantTolerance,
            nameof(determinantTolerance));

        HalfSpaceTolerance = ValidateNonNegative(
            halfSpaceTolerance,
            nameof(halfSpaceTolerance));

        PlaneMembershipTolerance = ValidatePositive(
            planeMembershipTolerance,
            nameof(planeMembershipTolerance));

        VertexMergeTolerance = ValidatePositive(
            vertexMergeTolerance,
            nameof(vertexMergeTolerance));

        InteriorTolerance = ValidateNonNegative(
            interiorTolerance,
            nameof(interiorTolerance));

        MinimumFaceArea = ValidatePositive(
            minimumFaceArea,
            nameof(minimumFaceArea));

        MinimumExtent = ValidatePositive(
            minimumExtent,
            nameof(minimumExtent));

        MinimumVolume = ValidatePositive(
            minimumVolume,
            nameof(minimumVolume));
    }

    public double DeterminantTolerance { get; }

    public double HalfSpaceTolerance { get; }

    public double PlaneMembershipTolerance { get; }

    public double VertexMergeTolerance { get; }

    public double InteriorTolerance { get; }

    public double MinimumFaceArea { get; }

    public double MinimumExtent { get; }

    public double MinimumVolume { get; }

    private static double ValidateNonNegative(
        double value,
        string parameterName)
    {
        if (
            !NumericTolerances.IsFinite(value) ||
            value < 0.0
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and non-negative.");
        }

        return value;
    }

    private static double ValidatePositive(
        double value,
        string parameterName)
    {
        if (
            !NumericTolerances.IsFinite(value) ||
            value <= 0.0
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The value must be finite and greater than zero.");
        }

        return value;
    }
}
