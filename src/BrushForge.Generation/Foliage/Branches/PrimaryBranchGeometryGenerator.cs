using BrushForge.Core.Numerics;
using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Geometry;

namespace BrushForge.Generation.Foliage.Branches;

/// <summary>
/// Realizes the current skeleton's primary branches as low-face-count
/// tapered brushes. Secondary branches remain planned-only until the next
/// hierarchy realization milestone.
/// </summary>
internal static class PrimaryBranchGeometryGenerator
{
    public static GeneratedTreeBrush[] Generate(
        TreeGenerationSettings settings,
        TreeBranchSkeleton skeleton,
        Vector3d trunkBaseCenter,
        Vector3d trunkTopCenter,
        double realizedTrunkWidth,
        double realizedCanopyWidth)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(skeleton);

        if (!trunkBaseCenter.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkBaseCenter),
                trunkBaseCenter,
                "The trunk base center must be finite.");
        }

        if (!trunkTopCenter.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(trunkTopCenter),
                trunkTopCenter,
                "The trunk top center must be finite.");
        }

        ValidatePositiveDimension(
            realizedTrunkWidth,
            nameof(realizedTrunkWidth));
        ValidatePositiveDimension(
            realizedCanopyWidth,
            nameof(realizedCanopyWidth));

        PlannedTreeBranch[] primaryBranches =
            skeleton.Branches
                .Where(
                    branch =>
                        branch.Depth == 0)
                .ToArray();
        GeneratedTreeBrush[] parts =
            new GeneratedTreeBrush[primaryBranches.Length];
        Vector3d trunkAxis =
            trunkTopCenter - trunkBaseCenter;
        double minimumHalfExtent =
            settings.GridSpacing.Units /
            4.0;

        for (
            int branchIndex = 0;
            branchIndex < primaryBranches.Length;
            branchIndex++
        ) {
            PlannedTreeBranch branch =
                primaryBranches[branchIndex];
            Vector3d startCenter =
                trunkBaseCenter +
                (trunkAxis * branch.AttachmentFraction);
            double branchLength = Math.Max(
                settings.GridSpacing.Units * 2.0,
                realizedCanopyWidth *
                0.5 *
                branch.LengthScale);
            Vector3d endCenter =
                startCenter +
                (CreateDirection(branch) * branchLength);
            double startHalfExtent = Math.Max(
                minimumHalfExtent,
                realizedTrunkWidth *
                0.5 *
                branch.StartRadiusScale);
            double endHalfExtent = Math.Max(
                minimumHalfExtent,
                realizedTrunkWidth *
                0.5 *
                branch.EndRadiusScale);
            ConvexBrush brush =
                OrientedSquareFrustumBrushFactory.Create(
                    startCenter,
                    endCenter,
                    startHalfExtent,
                    endHalfExtent,
                    settings.TrunkTextureName);
            Bounds3d bounds =
                Bounds3d.FromPoints(
                [
                    startCenter,
                    endCenter
                ])
                .Expand(
                    Math.Max(
                        startHalfExtent,
                        endHalfExtent));

            parts[branchIndex] =
                new GeneratedTreeBrush(
                    TreeBrushRole.Branch,
                    canopyLayerIndex: -1,
                    brush,
                    bounds);
        }

        return parts;
    }

    private static Vector3d CreateDirection(
        PlannedTreeBranch branch)
    {
        double azimuthRadians =
            DegreesToRadians(
                branch.AzimuthDegrees);
        double elevationRadians =
            DegreesToRadians(
                branch.ElevationDegrees);
        double horizontalMagnitude =
            Math.Cos(elevationRadians);

        return new Vector3d(
            Math.Cos(azimuthRadians) *
            horizontalMagnitude,
            Math.Sin(azimuthRadians) *
            horizontalMagnitude,
            Math.Sin(elevationRadians))
            .Normalize();
    }

    private static double DegreesToRadians(
        double degrees)
    {
        return degrees *
            (Math.PI / 180.0);
    }

    private static void ValidatePositiveDimension(
        double value,
        string parameterName)
    {
        if (
            !NumericTolerances.IsFinite(value) ||
            value <= NumericTolerances.Coordinate
        ) {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The realized tree dimension must be finite and greater than zero.");
        }
    }
}
