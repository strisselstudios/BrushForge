using BrushForge.Core.Numerics;
using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Geometry;

namespace BrushForge.Generation.Foliage.Branches;

/// <summary>
/// Resolves the stable branch skeleton into arbitrary-axis tapered brushes.
/// Geometry for the complete skeleton is resolved before Detail filters which
/// branches are realized, so increasing Detail reveals branches without
/// rerolling or moving the existing hierarchy.
/// </summary>
internal static class TreeBranchGeometryGenerator
{
    private const double ParallelReferenceThreshold = 0.90;

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

        Dictionary<string, ResolvedBranchGeometry> resolvedBranches =
            new(
                skeleton.Count,
                StringComparer.Ordinal);
        List<GeneratedTreeBrush> realizedParts =
            new(skeleton.Count);
        Vector3d trunkAxis =
            trunkTopCenter - trunkBaseCenter;
        double minimumHalfExtent =
            settings.GridSpacing.Units /
            4.0;

        foreach (PlannedTreeBranch branch in skeleton.Branches) {
            ResolvedBranchGeometry geometry =
                branch.Depth == 0
                    ? ResolvePrimaryBranch(
                        branch,
                        trunkBaseCenter,
                        trunkAxis,
                        realizedTrunkWidth,
                        realizedCanopyWidth,
                        minimumHalfExtent,
                        settings.GridSpacing.Units)
                    : ResolveChildBranch(
                        branch,
                        resolvedBranches,
                        minimumHalfExtent,
                        settings.GridSpacing.Units);

            resolvedBranches.Add(
                branch.Path,
                geometry);

            if (settings.Detail < branch.RequiredDetail) {
                continue;
            }

            ConvexBrush brush =
                OrientedSquareFrustumBrushFactory.Create(
                    geometry.StartCenter,
                    geometry.EndCenter,
                    geometry.StartHalfExtent,
                    geometry.EndHalfExtent,
                    settings.TrunkTextureName);
            Bounds3d bounds =
                Bounds3d.FromPoints(
                [
                    geometry.StartCenter,
                    geometry.EndCenter
                ])
                .Expand(
                    Math.Max(
                        geometry.StartHalfExtent,
                        geometry.EndHalfExtent));

            realizedParts.Add(
                new GeneratedTreeBrush(
                    TreeBrushRole.Branch,
                    canopyLayerIndex: -1,
                    brush,
                    bounds,
                    branch.Path));
        }

        return realizedParts.ToArray();
    }

    private static ResolvedBranchGeometry ResolvePrimaryBranch(
        PlannedTreeBranch branch,
        Vector3d trunkBaseCenter,
        Vector3d trunkAxis,
        double realizedTrunkWidth,
        double realizedCanopyWidth,
        double minimumHalfExtent,
        double grid)
    {
        Vector3d startCenter =
            trunkBaseCenter +
            (trunkAxis * branch.AttachmentFraction);
        double branchLength = Math.Max(
            grid * 2.0,
            realizedCanopyWidth *
            0.5 *
            branch.LengthScale);
        Vector3d endCenter =
            startCenter +
            (CreatePrimaryDirection(branch) * branchLength);
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

        return new ResolvedBranchGeometry(
            startCenter,
            endCenter,
            startHalfExtent,
            endHalfExtent);
    }

    private static ResolvedBranchGeometry ResolveChildBranch(
        PlannedTreeBranch branch,
        Dictionary<string, ResolvedBranchGeometry> resolvedBranches,
        double minimumHalfExtent,
        double grid)
    {
        if (
            branch.ParentPath is null ||
            !resolvedBranches.TryGetValue(
                branch.ParentPath,
                out ResolvedBranchGeometry parent)
        ) {
            throw new InvalidOperationException(
                $"The parent geometry for branch '{branch.Path}' was not resolved before its child.");
        }

        Vector3d parentAxisVector =
            parent.EndCenter - parent.StartCenter;
        double parentLength =
            parentAxisVector.Length;
        Vector3d parentDirection =
            parentAxisVector.Normalize();
        Vector3d startCenter =
            parent.StartCenter +
            (parentAxisVector * branch.AttachmentFraction);
        double branchLength = Math.Max(
            grid * 2.0,
            parentLength * branch.LengthScale);
        Vector3d endCenter =
            startCenter +
            (
                CreateChildDirection(
                    parentDirection,
                    branch) *
                branchLength
            );
        double parentHalfExtent =
            Interpolate(
                parent.StartHalfExtent,
                parent.EndHalfExtent,
                branch.AttachmentFraction);
        double startHalfExtent = Math.Max(
            minimumHalfExtent,
            parentHalfExtent *
            branch.StartRadiusScale);
        double endHalfExtent = Math.Max(
            minimumHalfExtent,
            parentHalfExtent *
            branch.EndRadiusScale);

        return new ResolvedBranchGeometry(
            startCenter,
            endCenter,
            startHalfExtent,
            endHalfExtent);
    }

    private static Vector3d CreatePrimaryDirection(
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

    private static Vector3d CreateChildDirection(
        Vector3d parentDirection,
        PlannedTreeBranch branch)
    {
        Vector3d reference =
            Math.Abs(
                Vector3d.Dot(
                    parentDirection,
                    Vector3d.UnitZ)) <
                ParallelReferenceThreshold
                ? Vector3d.UnitZ
                : Vector3d.UnitY;
        Vector3d right =
            Vector3d.Cross(
                reference,
                parentDirection)
                .Normalize();
        Vector3d up =
            Vector3d.Cross(
                parentDirection,
                right)
                .Normalize();
        double azimuthRadians =
            DegreesToRadians(
                branch.AzimuthDegrees);
        double deflectionRadians =
            DegreesToRadians(
                branch.ElevationDegrees);
        Vector3d radialDirection =
            (
                right *
                Math.Cos(azimuthRadians)
            ) +
            (
                up *
                Math.Sin(azimuthRadians)
            );

        return (
            (
                parentDirection *
                Math.Cos(deflectionRadians)
            ) +
            (
                radialDirection *
                Math.Sin(deflectionRadians)
            ))
            .Normalize();
    }

    private static double Interpolate(
        double start,
        double end,
        double fraction)
    {
        return start +
            ((end - start) * fraction);
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

    private readonly record struct ResolvedBranchGeometry(
        Vector3d StartCenter,
        Vector3d EndCenter,
        double StartHalfExtent,
        double EndHalfExtent);
}
