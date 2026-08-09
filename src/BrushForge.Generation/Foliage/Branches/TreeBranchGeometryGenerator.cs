using BrushForge.Core.Numerics;
using BrushForge.Geometry.Bounds;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.Generation.Geometry;

namespace BrushForge.Generation.Foliage.Branches;

/// <summary>
/// Resolves the stable branch skeleton into arbitrary-axis tapered brush paths.
/// The skeleton remains Detail-independent while Detail controls how many
/// connected segments are used to realize each visible branch path.
/// </summary>
internal static class TreeBranchGeometryGenerator
{
    private const double ParallelReferenceThreshold = 0.90;
    private const double PrimaryMaximumBendFraction = 0.12;
    private const double ChildMaximumBendFraction = 0.16;
    private const double MinimumChildOutwardComponent = 0.30;
    private const double MinimumChildUpwardComponent = 0.08;

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
            new(skeleton.Count * 2);
        Vector3d trunkAxis =
            trunkTopCenter - trunkBaseCenter;
        double minimumHalfExtent =
            settings.GridSpacing.Units /
            4.0;

        foreach (PlannedTreeBranch branch in skeleton.Branches) {
            int realizedSegmentCount =
                TreeDetailRealizationPolicy.ResolveBranchSegmentCount(
                    settings,
                    branch);
            int geometrySegmentCount =
                Math.Max(
                    1,
                    realizedSegmentCount);
            ResolvedBranchGeometry geometry =
                branch.Depth == 0
                    ? ResolvePrimaryBranch(
                        branch,
                        trunkBaseCenter,
                        trunkAxis,
                        realizedTrunkWidth,
                        realizedCanopyWidth,
                        minimumHalfExtent,
                        settings.GridSpacing.Units,
                        geometrySegmentCount)
                    : ResolveChildBranch(
                        branch,
                        resolvedBranches,
                        trunkBaseCenter,
                        trunkTopCenter,
                        minimumHalfExtent,
                        settings.GridSpacing.Units,
                        geometrySegmentCount);

            resolvedBranches.Add(
                branch.Path,
                geometry);

            if (realizedSegmentCount == 0) {
                continue;
            }

            AddRealizedSegments(
                realizedParts,
                settings,
                branch,
                geometry,
                realizedSegmentCount);
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
        double grid,
        int realizedSegmentCount)
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

        return CreateResolvedGeometry(
            branch,
            startCenter,
            endCenter,
            startHalfExtent,
            endHalfExtent,
            realizedSegmentCount);
    }

    private static ResolvedBranchGeometry ResolveChildBranch(
        PlannedTreeBranch branch,
        Dictionary<string, ResolvedBranchGeometry> resolvedBranches,
        Vector3d trunkBaseCenter,
        Vector3d trunkTopCenter,
        double minimumHalfExtent,
        double grid,
        int realizedSegmentCount)
    {
        if (
            branch.ParentPath is null ||
            !resolvedBranches.TryGetValue(
                branch.ParentPath,
                out ResolvedBranchGeometry? parent) ||
            parent is null
        ) {
            throw new InvalidOperationException(
                $"The parent geometry for branch '{branch.Path}' was not resolved before its child.");
        }

        PathSample parentSample =
            SamplePath(
                parent.MaximumDetailCenters,
                branch.AttachmentFraction);
        double branchLength = Math.Max(
            grid * 2.0,
            parent.ChordLength * branch.LengthScale);
        Vector3d outwardDirection =
            CreateHorizontalOutwardDirection(
                parentSample.Position,
                parentSample.Direction,
                trunkBaseCenter,
                trunkTopCenter);
        Vector3d childDirection =
            CreateChildDirection(
                parentSample.Direction,
                branch,
                outwardDirection);
        Vector3d endCenter =
            parentSample.Position +
            (childDirection * branchLength);
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

        return CreateResolvedGeometry(
            branch,
            parentSample.Position,
            endCenter,
            startHalfExtent,
            endHalfExtent,
            realizedSegmentCount,
            outwardDirection);
    }

    private static ResolvedBranchGeometry CreateResolvedGeometry(
        PlannedTreeBranch branch,
        Vector3d startCenter,
        Vector3d endCenter,
        double startHalfExtent,
        double endHalfExtent,
        int realizedSegmentCount,
        Vector3d? childOutwardDirection = null)
    {
        Vector3d[] maximumDetailPath =
            CreateMaximumDetailPath(
                branch,
                startCenter,
                endCenter,
                childOutwardDirection);
        Vector3d[] realizedPath =
            CreateRealizedPath(
                maximumDetailPath,
                realizedSegmentCount);

        return new ResolvedBranchGeometry(
            maximumDetailPath,
            realizedPath,
            startHalfExtent,
            endHalfExtent,
            startCenter.DistanceTo(endCenter));
    }

    private static void AddRealizedSegments(
        List<GeneratedTreeBrush> realizedParts,
        TreeGenerationSettings settings,
        PlannedTreeBranch branch,
        ResolvedBranchGeometry geometry,
        int realizedSegmentCount)
    {
        for (
            int segmentIndex = 0;
            segmentIndex < realizedSegmentCount;
            segmentIndex++
        ) {
            double startFraction =
                segmentIndex /
                (double)realizedSegmentCount;
            double endFraction =
                (segmentIndex + 1) /
                (double)realizedSegmentCount;
            Vector3d startCenter =
                geometry.RealizedCenters[segmentIndex];
            Vector3d endCenter =
                geometry.RealizedCenters[segmentIndex + 1];
            double startHalfExtent =
                Interpolate(
                    geometry.StartHalfExtent,
                    geometry.EndHalfExtent,
                    startFraction);
            double endHalfExtent =
                Interpolate(
                    geometry.StartHalfExtent,
                    geometry.EndHalfExtent,
                    endFraction);
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

            realizedParts.Add(
                new GeneratedTreeBrush(
                    TreeBrushRole.Branch,
                    canopyLayerIndex: -1,
                    brush,
                    bounds,
                    branch.Path,
                    segmentIndex,
                    realizedSegmentCount));
        }
    }

    private static Vector3d[] CreateMaximumDetailPath(
        PlannedTreeBranch branch,
        Vector3d startCenter,
        Vector3d endCenter,
        Vector3d? childOutwardDirection)
    {
        int maximumSegmentCount =
            TreeDetailRealizationPolicy.ResolveBranchMaximumSegmentCount(
                branch);
        Vector3d[] centers =
            new Vector3d[maximumSegmentCount + 1];

        centers[0] = startCenter;
        centers[^1] = endCenter;

        if (maximumSegmentCount == 1) {
            return centers;
        }

        Vector3d axisVector =
            endCenter - startCenter;
        double length =
            axisVector.Length;
        Vector3d axis =
            axisVector.Normalize();
        (Vector3d right, Vector3d up) =
            CreatePerpendicularBasis(axis);
        double phaseRadians =
            DegreesToRadians(
                branch.AzimuthDegrees +
                (branch.ElevationDegrees * 0.5) +
                (branch.Depth * 53.0));
        Vector3d bendDirection =
            (
                (right * Math.Cos(phaseRadians)) +
                (up * Math.Sin(phaseRadians))
            )
            .Normalize();

        if (childOutwardDirection is Vector3d outwardDirection) {
            bendDirection =
                CreateGenericChildBendDirection(
                    bendDirection,
                    outwardDirection);
        }

        double maximumBendFraction =
            branch.Depth == 0
                ? PrimaryMaximumBendFraction
                : ChildMaximumBendFraction;

        for (
            int pointIndex = 1;
            pointIndex < maximumSegmentCount;
            pointIndex++
        ) {
            double fraction =
                pointIndex /
                (double)maximumSegmentCount;
            double bendMagnitude =
                length *
                maximumBendFraction *
                Math.Sin(
                    Math.PI * fraction);

            centers[pointIndex] =
                startCenter +
                (axisVector * fraction) +
                (bendDirection * bendMagnitude);
        }

        return centers;
    }

    private static Vector3d[] CreateRealizedPath(
        Vector3d[] maximumDetailPath,
        int realizedSegmentCount)
    {
        if (realizedSegmentCount <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(realizedSegmentCount),
                realizedSegmentCount,
                "A realized branch path requires at least one segment.");
        }

        int maximumSegmentCount =
            maximumDetailPath.Length - 1;

        if (realizedSegmentCount > maximumSegmentCount) {
            throw new ArgumentOutOfRangeException(
                nameof(realizedSegmentCount),
                realizedSegmentCount,
                "A realized branch path cannot exceed its maximum segment count.");
        }

        if (realizedSegmentCount == maximumSegmentCount) {
            return maximumDetailPath.ToArray();
        }

        Vector3d[] centers =
            new Vector3d[realizedSegmentCount + 1];

        for (
            int pointIndex = 0;
            pointIndex <= realizedSegmentCount;
            pointIndex++
        ) {
            double fraction =
                pointIndex /
                (double)realizedSegmentCount;

            centers[pointIndex] =
                SamplePath(
                    maximumDetailPath,
                    fraction)
                .Position;
        }

        return centers;
    }

    private static PathSample SamplePath(
        Vector3d[] centers,
        double fraction)
    {
        if (centers.Length < 2) {
            throw new ArgumentException(
                "A branch path requires at least two centers.",
                nameof(centers));
        }

        double clampedFraction =
            Math.Clamp(
                fraction,
                0.0,
                1.0);
        double scaledPosition =
            clampedFraction *
            (centers.Length - 1);
        int segmentIndex =
            clampedFraction >= 1.0
                ? centers.Length - 2
                : (int)Math.Floor(
                    scaledPosition);
        double localFraction =
            clampedFraction >= 1.0
                ? 1.0
                : scaledPosition - segmentIndex;
        Vector3d segmentStart =
            centers[segmentIndex];
        Vector3d segmentEnd =
            centers[segmentIndex + 1];
        Vector3d direction =
            (segmentEnd - segmentStart)
                .Normalize();
        Vector3d position =
            segmentStart +
            (
                (segmentEnd - segmentStart) *
                localFraction
            );

        return new PathSample(
            position,
            direction);
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
        PlannedTreeBranch branch,
        Vector3d outwardDirection)
    {
        (Vector3d right, Vector3d up) =
            CreatePerpendicularBasis(
                parentDirection);
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
        Vector3d candidate =
            (
                (
                    parentDirection *
                    Math.Cos(deflectionRadians)
                ) +
                (
                    radialDirection *
                    Math.Sin(deflectionRadians)
                ))
            .Normalize();
        return ConstrainGenericChildDirection(
            candidate,
            outwardDirection);
    }

    private static Vector3d CreateHorizontalOutwardDirection(
        Vector3d attachmentPosition,
        Vector3d parentDirection,
        Vector3d trunkBaseCenter,
        Vector3d trunkTopCenter)
    {
        double verticalSpan =
            trunkTopCenter.Z -
            trunkBaseCenter.Z;
        double trunkFraction =
            Math.Abs(verticalSpan) <=
            NumericTolerances.UnitVector
                ? 0.5
                : Math.Clamp(
                    (attachmentPosition.Z -
                        trunkBaseCenter.Z) /
                    verticalSpan,
                    0.0,
                    1.0);
        Vector3d trunkCenter =
            trunkBaseCenter +
            ((trunkTopCenter - trunkBaseCenter) *
                trunkFraction);
        Vector3d horizontalRadial =
            new(
                attachmentPosition.X - trunkCenter.X,
                attachmentPosition.Y - trunkCenter.Y,
                0.0);

        if (
            horizontalRadial.Length >
            NumericTolerances.UnitVector
        ) {
            return horizontalRadial.Normalize();
        }

        Vector3d parentHorizontal =
            new(
                parentDirection.X,
                parentDirection.Y,
                0.0);

        if (
            parentHorizontal.Length >
            NumericTolerances.UnitVector
        ) {
            return parentHorizontal.Normalize();
        }

        return Vector3d.UnitX;
    }

    private static Vector3d ConstrainGenericChildDirection(
        Vector3d candidate,
        Vector3d outwardDirection)
    {
        Vector3d horizontalTangent =
            Vector3d.Cross(
                Vector3d.UnitZ,
                outwardDirection)
                .Normalize();
        double outwardComponent =
            Math.Max(
                MinimumChildOutwardComponent,
                Vector3d.Dot(
                    candidate,
                    outwardDirection));
        double maximumOutwardComponent =
            Math.BitDecrement(
                Math.Sqrt(
                    1.0 -
                    (MinimumChildUpwardComponent *
                        MinimumChildUpwardComponent)));
        outwardComponent =
            Math.Min(
                outwardComponent,
                maximumOutwardComponent);
        double maximumUpwardComponent =
            Math.Sqrt(
                Math.Max(
                    0.0,
                    1.0 -
                    (outwardComponent * outwardComponent)));
        double upwardComponent =
            Math.Clamp(
                Math.Max(
                    MinimumChildUpwardComponent,
                    candidate.Z),
                MinimumChildUpwardComponent,
                maximumUpwardComponent);
        double remainingSquared =
            Math.Max(
                0.0,
                1.0 -
                (outwardComponent * outwardComponent) -
                (upwardComponent * upwardComponent));
        double tangentSign =
            Vector3d.Dot(
                candidate,
                horizontalTangent) < 0.0
                ? -1.0
                : 1.0;
        double tangentComponent =
            Math.Sqrt(remainingSquared) *
            tangentSign;

        return (
            (outwardDirection * outwardComponent) +
            (Vector3d.UnitZ * upwardComponent) +
            (horizontalTangent * tangentComponent))
            .Normalize();
    }

    private static Vector3d CreateGenericChildBendDirection(
        Vector3d candidate,
        Vector3d outwardDirection)
    {
        Vector3d horizontalTangent =
            Vector3d.Cross(
                Vector3d.UnitZ,
                outwardDirection)
                .Normalize();
        double tangentSign =
            Vector3d.Dot(
                candidate,
                horizontalTangent) < 0.0
                ? -1.0
                : 1.0;

        return horizontalTangent *
            tangentSign;
    }

    private static (Vector3d Right, Vector3d Up) CreatePerpendicularBasis(
        Vector3d direction)
    {
        Vector3d reference =
            Math.Abs(
                Vector3d.Dot(
                    direction,
                    Vector3d.UnitZ)) <
                ParallelReferenceThreshold
                ? Vector3d.UnitZ
                : Vector3d.UnitY;
        Vector3d right =
            Vector3d.Cross(
                reference,
                direction)
                .Normalize();
        Vector3d up =
            Vector3d.Cross(
                direction,
                right)
                .Normalize();

        return (
            right,
            up);
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

    private sealed record ResolvedBranchGeometry(
        Vector3d[] MaximumDetailCenters,
        Vector3d[] RealizedCenters,
        double StartHalfExtent,
        double EndHalfExtent,
        double ChordLength);

    private readonly record struct PathSample(
        Vector3d Position,
        Vector3d Direction);
}
