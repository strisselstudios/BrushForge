using System.Globalization;
using BrushForge.Core.Numerics;

namespace BrushForge.MapFormat.Serialization;

/// <summary>
/// Culture-invariant canonical number formatting for Valve map output.
/// </summary>
public static class Valve220NumberFormatter
{
    private const string FormatPattern =
        "0.#################";

    public static string Format(double value)
    {
        if (!NumericTolerances.IsFinite(value)) {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Valve map numbers must be finite.");
        }

        if (value == 0.0) {
            return "0";
        }

        string formatted =
            value.ToString(
                FormatPattern,
                CultureInfo.InvariantCulture);

        return formatted == "-0"
            ? "0"
            : formatted;
    }
}
