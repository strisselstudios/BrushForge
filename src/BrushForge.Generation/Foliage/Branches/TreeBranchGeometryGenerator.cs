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
    private const double PrimarySecondaryBendFraction = 0.035;
    private const double ChildSecondaryBendFraction = 0.05;
    private const double MinimumChildOutwardComponent = 0.30;
    private const double MinimumChildUpwardComponent = 0.08;
    private const double ReferenceTrunkDiameter = 32.0;
    private const double ReferenceTrunkHalfWidth = 16.0;
    private const double MaximumPrimaryAllometricScale = 2.0;
    private const double PrimaryLengthExtension = 1.25;
    private const double ChildLengthExtension = 1.15;
    private const double MinimumPrimarySlenderness = 3.0;
    private const double MinimumChildSlenderness = 3.0;
    private const double MaximumPrimaryCanopyLengthFraction = 0.85;
    private const double MaximumChildParentLengthFraction = 0.85;
    private const double MaximumJunctionInsetFraction = 0.55;
    private const double MaximumParentInsetFraction = 0.18;
    private const double ReferenceCanopyWidthToTreeHeightRatio = 0.75;
    private const double MaximumOversizedCanopyReachScale = 1.50;

    public static GeneratedTreeBrush[] Generate(
        TreeGenerationSettings settings,
        TreeBranchSkeleton skeleton,
        GeneratedTrunk trunk,
        double realizedCanopyWidth)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(skeleton);
        ArgumentNullException.ThrowIfNull(trunk);

        ValidatePositiveDimension(
            realizedCanopyWidth,
            nameof(realizedCanopyWidth));

        Dictionary<string, ResolvedBranchGeometry> resolvedBranches =
            new(
                skeleton.Count,
                StringComparer.Ordinal);
        List<GeneratedTreeBrush> realizedParts =
            new(skeleton.Count * 2);
        double minimumHalfExtent =
            settings.GridSpacing.Units /
            4.0;
        double canopyReachWidth =
            ResolveCanopyReachWidth(
                realizedCanopyWidth,
                settings.OverallHeight);

        foreach (PlannedTreeBranch branch in skeleton.Branches) {
            int realizedSegmentCount =
                TreeDetailRealizationPolicy.ResolveBranchSegmentCount(
                    settings,
                    branch);
            int geometrySegmentCount =
                Math.Max(
                    1,
                    realizedSegmentCount);
            int crossSectionSideCount =
                TreeDetailRealizationPolicy.ResolveBranchSideCount(
                    settings,
                    branch);
            ResolvedBranchGeometry geometry =
                branch.Depth == 0
                    ? ResolvePrimaryBranch(
                        branch,
                        trunk.AttachmentProfile,
                        canopyReachWidth,
                        minimumHalfExtent,
                        settings.GridSpacing.Units,
                        geometrySegmentCount,
                        crossSectionSideCount)
                    : ResolveChildBranch(
                        branch,
                        resolvedBranches,
                        trunk.AttachmentProfile,
                        minimumHalfExtent,
                        settings.GridSpacing.Units,
                        geometrySegmentCount,
                        crossSectionSideCount);

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
        TrunkAttachmentProfile trunkProfile,
        double canopyReachWidth,
        double minimumHalfExtent,
        double grid,
        int realizedSegmentCount,
        int crossSectionSideCount)
    {
        Vector3d branchDirection =
            CreatePrimaryDirection(
                branch);
        TrunkAttachmentSample attachment =
            trunkProfile.SampleSurface(
                branch.AttachmentFraction,
                branchDirection);
        double structuralParentHalfWidth =
            ResolvePrimaryStructuralParentHalfWidth(
                attachment.HalfWidth);
        double startHalfExtent = Math.Max(
            minimumHalfExtent,
            structuralParentHalfWidth *
            branch.StartRadiusScale);
        double endHalfExtent = Math.Max(
            minimumHalfExtent,
            structuralParentHalfWidth *
            branch.EndRadiusScale);
        double branchLength =
            ResolvePrimaryBranchLength(
                branch,
                attachment.HalfWidth * 2.0,
                canopyReachWidth,
                startHalfExtent,
                grid);
        Vector3d horizontalOutwardDirection =
            CreateHorizontalDirection(
                branchDirection);
        double junctionInset =
            ResolveJunctionInset(
                startHalfExtent,
                attachment.HalfWidth);
        Vector3d startCenter =
            attachment.SurfacePoint -
            (horizontalOutwardDirection * junctionInset);
        Vector3d endCenter =
            attachment.SurfacePoint +
            (branchDirection * branchLength);

        return CreateResolvedGeometry(
            branch,
            startCenter,
            endCenter,
            startHalfExtent,
            endHalfExtent,
            realizedSegmentCount,
            crossSectionSideCount);
    }

    private static ResolvedBranchGeometry ResolveChildBranch(
        PlannedTreeBranch branch,
        Dictionary<string, ResolvedBranchGeometry> resolvedBranches,
        TrunkAttachmentProfile trunkProfile,
        double minimumHalfExtent,
        double grid,
        int realizedSegmentCount,
        int crossSectionSideCount)
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
        Vector3d outwardDirection =
            CreateHorizontalOutwardDirection(
                parentSample.Position,
                parentSample.Direction,
                trunkProfile);
        Vector3d childDirection =
            CreateChildDirection(
                parentSample.Direction,
                branch,
                outwardDirection);
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
        Vector3d parentSurfacePoint =
            CreateRegularParentSurfacePoint(
                parentSample.Position,
                parentSample.Direction,
                childDirection,
                parentHalfExtent,
                parent.CrossSectionSideCount);
        Vector3d radialDirection =
            (parentSurfacePoint - parentSample.Position)
                .Normalize();
        double junctionInset =
            ResolveJunctionInset(
                startHalfExtent,
                parentHalfExtent);
        Vector3d startCenter =
            parentSurfacePoint -
            (radialDirection * junctionInset);
        double branchLength =
            ResolveChildBranchLength(
                branch,
                parent.ChordLength,
                startHalfExtent,
                grid);
        Vector3d endCenter =
            parentSurfacePoint +
            (childDirection * branchLength);

        return CreateResolvedGeometry(
            branch,
            startCenter,
            endCenter,
            startHalfExtent,
            endHalfExtent,
            realizedSegmentCount,
            crossSectionSideCount,
            outwardDirection);
    }

    private static double ResolveCanopyReachWidth(
        double realizedCanopyWidth,
        double overallHeight)
    {
        ValidatePositiveDimension(
            realizedCanopyWidth,
            nameof(realizedCanopyWidth));
        ValidatePositiveDimension(
            overallHeight,
            nameof(overallHeight));

        double referenceCanopyWidth =
            overallHeight *
            ReferenceCanopyWidthToTreeHeightRatio;

        if (realizedCanopyWidth <= referenceCanopyWidth) {
            return realizedCanopyWidth;
        }

        double oversizedRatio =
            realizedCanopyWidth /
            referenceCanopyWidth;
        double responsiveScale =
            Math.Min(
                MaximumOversizedCanopyReachScale,
                Math.Sqrt(
                    oversizedRatio));

        return referenceCanopyWidth *
            responsiveScale;
    }

    private static double ResolvePrimaryBranchLength(
        PlannedTreeBranch branch,
        double localTrunkDiameter,
        double canopyReachWidth,
        double startHalfExtent,
        double grid)
    {
        double diameterRatio =
            Math.Max(
                1.0,
                localTrunkDiameter /
                ReferenceTrunkDiameter);
        double allometricScale =
            Math.Min(
                MaximumPrimaryAllometricScale,
                Math.Sqrt(
                    diameterRatio));
        double canopyDrivenLength =
            canopyReachWidth *
            0.5 *
            branch.LengthScale *
            allometricScale *
            PrimaryLengthExtension;
        double slendernessDrivenLength =
            startHalfExtent *
            2.0 *
            MinimumPrimarySlenderness;
        double maximumLength =
            Math.Max(
                grid * 2.0,
                canopyReachWidth *
                MaximumPrimaryCanopyLengthFraction);

        return ResolveBoundedBranchLength(
            canopyDrivenLength,
            slendernessDrivenLength,
            maximumLength,
            grid);
    }

    private static double ResolveChildBranchLength(
        PlannedTreeBranch branch,
        double parentChordLength,
        double startHalfExtent,
        double grid)
    {
        double parentDrivenLength =
            parentChordLength *
            branch.LengthScale *
            ChildLengthExtension;
        double slendernessDrivenLength =
            startHalfExtent *
            2.0 *
            MinimumChildSlenderness;
        double maximumLength =
            Math.Max(
                grid * 2.0,
                parentChordLength *
                MaximumChildParentLengthFraction);

        return ResolveBoundedBranchLength(
            parentDrivenLength,
            slendernessDrivenLength,
            maximumLength,
            grid);
    }

    private static double ResolveBoundedBranchLength(
        double structuralLength,
        double slendernessLength,
        double maximumLength,
        double grid)
    {
        double minimumLength =
            grid * 2.0;
        double desiredLength =
            Math.Max(
                minimumLength,
                Math.Max(
                    structuralLength,
                    slendernessLength));

        return Math.Min(
            desiredLength,
            maximumLength);
    }

    private static ResolvedBranchGeometry CreateResolvedGeometry(
        PlannedTreeBranch branch,
        Vector3d startCenter,
        Vector3d endCenter,
        double startHalfExtent,
        double endHalfExtent,
        int realizedSegmentCount,
        int crossSectionSideCount,
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
            startCenter.DistanceTo(endCenter),
            crossSectionSideCount);
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
                OrientedConvexFrustumBrushFactory.Create(
                    startCenter,
                    endCenter,
                    startHalfExtent,
                    endHalfExtent,
                    geometry.CrossSectionSideCount,
                    settings.TrunkTextureName);
            double maximumApothem =
                Math.Max(
                    startHalfExtent,
                    endHalfExtent);
            double maximumCircumradius =
                maximumApothem /
                Math.Cos(
                    Math.PI /
                    geometry.CrossSectionSideCount);
            Bounds3d bounds =
                Bounds3d.FromPoints(
                [
                    startCenter,
                    endCenter
                ])
                .Expand(
                    maximumCircumradius);

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
        double secondaryBendFraction =
            branch.Depth == 0
                ? PrimarySecondaryBendFraction
                : ChildSecondaryBendFraction;
        Vector3d secondaryBendDirection =
            Vector3d.Cross(
                axis,
                bendDirection)
                .Normalize();

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
            double secondaryBendMagnitude =
                length *
                secondaryBendFraction *
                Math.Sin(
                    Math.PI *
                    2.0 *
                    fraction);

            centers[pointIndex] =
                startCenter +
                (axisVector * fraction) +
                (bendDirection * bendMagnitude) +
                (secondaryBendDirection * secondaryBendMagnitude);
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
        TrunkAttachmentProfile trunkProfile)
    {
        Vector3d trunkCenter =
            trunkProfile.SampleCenterAtHeight(
                attachmentPosition.Z);
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

    private static Vector3d CreateRegularParentSurfacePoint(
        Vector3d parentCenter,
        Vector3d parentDirection,
        Vector3d childDirection,
        double parentHalfExtent,
        int parentSideCount)
    {
        (Vector3d right, Vector3d up) =
            CreatePerpendicularBasis(
                parentDirection);
        Vector3d radial =
            childDirection -
            (
                parentDirection *
                Vector3d.Dot(
                    childDirection,
                    parentDirection)
            );

        if (
            radial.Length <=
            NumericTolerances.UnitVector
        ) {
            radial = right;
        }
        else {
            radial = radial.Normalize();
        }

        double rightComponent =
            Vector3d.Dot(
                radial,
                right);
        double upComponent =
            Vector3d.Dot(
                radial,
                up);
        double faceAngleStep =
            (Math.PI * 2.0) /
            parentSideCount;
        double firstFaceNormalAngle =
            -Math.PI +
            faceAngleStep;
        double maximumFaceDot =
            0.0;

        for (
            int faceIndex = 0;
            faceIndex < parentSideCount;
            faceIndex++
        ) {
            double faceAngle =
                firstFaceNormalAngle +
                (faceAngleStep * faceIndex);
            double faceDot =
                (rightComponent * Math.Cos(faceAngle)) +
                (upComponent * Math.Sin(faceAngle));

            maximumFaceDot =
                Math.Max(
                    maximumFaceDot,
                    faceDot);
        }

        double surfaceScale =
            parentHalfExtent /
            maximumFaceDot;

        return parentCenter +
            (radial * surfaceScale);
    }

    private static double ResolvePrimaryStructuralParentHalfWidth(
        double localParentHalfWidth)
    {
        if (
            localParentHalfWidth <=
            ReferenceTrunkHalfWidth
        ) {
            return localParentHalfWidth;
        }

        return ReferenceTrunkHalfWidth *
            Math.Sqrt(
                localParentHalfWidth /
                ReferenceTrunkHalfWidth);
    }

    private static double ResolveJunctionInset(
        double childHalfExtent,
        double parentHalfExtent)
    {
        return Math.Min(
            childHalfExtent *
                MaximumJunctionInsetFraction,
            parentHalfExtent *
                MaximumParentInsetFraction);
    }

    private static Vector3d CreateHorizontalDirection(
        Vector3d direction)
    {
        Vector3d horizontal =
            new(
                direction.X,
                direction.Y,
                0.0);

        if (
            horizontal.Length <=
            NumericTolerances.UnitVector
        ) {
            return Vector3d.UnitX;
        }

        return horizontal.Normalize();
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
        double ChordLength,
        int CrossSectionSideCount);

    private readonly record struct PathSample(
        Vector3d Position,
        Vector3d Direction);
}
