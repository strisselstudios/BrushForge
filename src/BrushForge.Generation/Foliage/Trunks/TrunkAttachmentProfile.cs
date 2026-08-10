using BrushForge.Core.Numerics;
using BrushForge.Geometry.Vectors;

namespace BrushForge.Generation.Foliage;

/// <summary>
/// Immutable sampled centerline and local cross-section widths for the
/// realized trunk. Branch geometry uses this profile so attachments follow
/// trunk taper and deformation instead of an approximate base-to-top axis.
/// </summary>
internal sealed class TrunkAttachmentProfile
{
    private readonly double[] _fractions;
    private readonly Vector3d[] _centers;
    private readonly double[] _halfWidths;

    public TrunkAttachmentProfile(
        double[] fractions,
        Vector3d[] centers,
        double[] halfWidths,
        TrunkCrossSectionProfile crossSection)
    {
        ArgumentNullException.ThrowIfNull(fractions);
        ArgumentNullException.ThrowIfNull(centers);
        ArgumentNullException.ThrowIfNull(halfWidths);

        if (
            fractions.Length < 2 ||
            fractions.Length != centers.Length ||
            fractions.Length != halfWidths.Length
        ) {
            throw new ArgumentException(
                "A trunk attachment profile requires matching arrays with at least two samples.",
                nameof(fractions));
        }

        if (!Enum.IsDefined(crossSection)) {
            throw new ArgumentOutOfRangeException(
                nameof(crossSection),
                crossSection,
                "The trunk cross-section profile is not supported.");
        }

        for (int index = 0; index < fractions.Length; index++) {
            double fraction = fractions[index];
            Vector3d center = centers[index];
            double halfWidth = halfWidths[index];

            if (
                !NumericTolerances.IsFinite(fraction) ||
                fraction < 0.0 ||
                fraction > 1.0
            ) {
                throw new ArgumentOutOfRangeException(
                    nameof(fractions),
                    fraction,
                    "Trunk profile fractions must be finite and in the range [0, 1].");
            }

            if (index > 0 && fraction <= fractions[index - 1]) {
                throw new ArgumentException(
                    "Trunk profile fractions must increase strictly from base to top.",
                    nameof(fractions));
            }

            if (!center.IsFinite) {
                throw new ArgumentOutOfRangeException(
                    nameof(centers),
                    center,
                    "Trunk profile centers must be finite.");
            }

            if (
                index > 0 &&
                center.Z <= centers[index - 1].Z
            ) {
                throw new ArgumentException(
                    "Trunk profile centers must increase strictly in height from base to top.",
                    nameof(centers));
            }

            if (
                !NumericTolerances.IsFinite(halfWidth) ||
                halfWidth <= NumericTolerances.Coordinate
            ) {
                throw new ArgumentOutOfRangeException(
                    nameof(halfWidths),
                    halfWidth,
                    "Trunk profile half widths must be finite and greater than zero.");
            }
        }

        if (
            !NumericTolerances.NearlyEqual(
                fractions[0],
                0.0) ||
            !NumericTolerances.NearlyEqual(
                fractions[^1],
                1.0)
        ) {
            throw new ArgumentException(
                "A trunk attachment profile must include exact base and top samples.",
                nameof(fractions));
        }

        _fractions = fractions.ToArray();
        _centers = centers.ToArray();
        _halfWidths = halfWidths.ToArray();
        CrossSection = crossSection;
    }

    public TrunkCrossSectionProfile CrossSection { get; }

    public TrunkAttachmentSample SampleSurface(
        double fraction,
        Vector3d outwardDirection)
    {
        CrossSectionSample crossSection =
            SampleCrossSection(fraction);
        Vector3d horizontalDirection =
            NormalizeHorizontalDirection(
                outwardDirection);
        double surfaceRadius =
            CalculateSurfaceRadius(
                crossSection.HalfWidth,
                horizontalDirection);

        return new TrunkAttachmentSample(
            crossSection.Center +
                (horizontalDirection * surfaceRadius),
            crossSection.HalfWidth);
    }

    public Vector3d SampleCenterAtHeight(double z)
    {
        if (!NumericTolerances.IsFinite(z)) {
            throw new ArgumentOutOfRangeException(
                nameof(z),
                z,
                "A trunk profile sample height must be finite.");
        }

        double clampedZ =
            Math.Clamp(
                z,
                _centers[0].Z,
                _centers[^1].Z);
        int segmentIndex =
            FindSegmentForHeight(
                clampedZ);
        Vector3d start =
            _centers[segmentIndex];
        Vector3d end =
            _centers[segmentIndex + 1];
        double localFraction =
            (clampedZ - start.Z) /
            (end.Z - start.Z);

        return start +
            ((end - start) * localFraction);
    }

    private CrossSectionSample SampleCrossSection(double fraction)
    {
        if (!NumericTolerances.IsFinite(fraction)) {
            throw new ArgumentOutOfRangeException(
                nameof(fraction),
                fraction,
                "A trunk attachment fraction must be finite.");
        }

        double clampedFraction =
            Math.Clamp(
                fraction,
                0.0,
                1.0);
        int segmentIndex =
            FindSegmentForFraction(
                clampedFraction);
        double startFraction =
            _fractions[segmentIndex];
        double endFraction =
            _fractions[segmentIndex + 1];
        double localFraction =
            (clampedFraction - startFraction) /
            (endFraction - startFraction);
        Vector3d center =
            _centers[segmentIndex] +
            (
                (_centers[segmentIndex + 1] -
                    _centers[segmentIndex]) *
                localFraction
            );
        double halfWidth =
            Interpolate(
                _halfWidths[segmentIndex],
                _halfWidths[segmentIndex + 1],
                localFraction);

        return new CrossSectionSample(
            center,
            halfWidth);
    }

    private int FindSegmentForFraction(double fraction)
    {
        if (fraction >= 1.0) {
            return _fractions.Length - 2;
        }

        for (
            int index = 0;
            index < _fractions.Length - 1;
            index++
        ) {
            if (fraction < _fractions[index + 1]) {
                return index;
            }
        }

        return _fractions.Length - 2;
    }

    private int FindSegmentForHeight(double z)
    {
        if (z >= _centers[^1].Z) {
            return _centers.Length - 2;
        }

        for (
            int index = 0;
            index < _centers.Length - 1;
            index++
        ) {
            if (z < _centers[index + 1].Z) {
                return index;
            }
        }

        return _centers.Length - 2;
    }

    private double CalculateSurfaceRadius(
        double halfWidth,
        Vector3d horizontalDirection)
    {
        double absoluteX =
            Math.Abs(
                horizontalDirection.X);
        double absoluteY =
            Math.Abs(
                horizontalDirection.Y);
        double radius =
            double.PositiveInfinity;

        if (absoluteX > NumericTolerances.UnitVector) {
            radius = Math.Min(
                radius,
                halfWidth / absoluteX);
        }

        if (absoluteY > NumericTolerances.UnitVector) {
            radius = Math.Min(
                radius,
                halfWidth / absoluteY);
        }

        if (CrossSection == TrunkCrossSectionProfile.Octagonal) {
            radius = Math.Min(
                radius,
                (halfWidth * 1.5) /
                (absoluteX + absoluteY));
        }

        return radius;
    }

    private static Vector3d NormalizeHorizontalDirection(
        Vector3d direction)
    {
        if (!direction.IsFinite) {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "A trunk attachment direction must be finite.");
        }

        Vector3d horizontal = new(
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

    private static double Interpolate(
        double start,
        double end,
        double fraction)
    {
        return start +
            ((end - start) * fraction);
    }

    private readonly record struct CrossSectionSample(
        Vector3d Center,
        double HalfWidth);
}

internal readonly record struct TrunkAttachmentSample(
    Vector3d SurfacePoint,
    double HalfWidth);
