using System.Text;

namespace BrushForge.MapFormat.Serialization;

/// <summary>
/// Escapes text for a quoted Valve map key or value.
/// </summary>
public static class Valve220StringCodec
{
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        StringBuilder builder =
            new(value.Length);

        foreach (char character in value) {
            switch (character) {
                case '\\':
                    builder.Append(@"\\");
                    break;

                case '"':
                    builder.Append("\\\"");
                    break;

                case '\0':
                case '\r':
                case '\n':
                    throw new ArgumentException(
                        "Valve map strings cannot contain null characters or line breaks.",
                        nameof(value));

                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }
}
