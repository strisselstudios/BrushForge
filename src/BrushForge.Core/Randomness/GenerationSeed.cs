using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BrushForge.Core.Randomness;

/// <summary>
/// Stable 64-bit seed used by deterministic BrushForge generators.
/// </summary>
public readonly record struct GenerationSeed(ulong Value)
{
    private const ulong FnvOffsetBasis = 14695981039346656037;
    private const ulong FnvPrime = 1099511628211;

    public static GenerationSeed FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        ulong hash = FnvOffsetBasis;

        unchecked {
            foreach (byte value in bytes) {
                hash ^= value;
                hash *= FnvPrime;
            }
        }

        return new GenerationSeed(hash);
    }

    public static GenerationSeed CreateRandom()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);

        return new GenerationSeed(
            BinaryPrimitives.ReadUInt64LittleEndian(bytes));
    }

    public static GenerationSeed Parse(string text)
    {
        if (!TryParse(text, out GenerationSeed seed)) {
            throw new FormatException(
                "The generation seed must be an unsigned 64-bit integer.");
        }

        return seed;
    }

    public static bool TryParse(
        string? text,
        out GenerationSeed seed)
    {
        if (
            ulong.TryParse(
                text,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out ulong value)
        ) {
            seed = new GenerationSeed(value);
            return true;
        }

        seed = default;
        return false;
    }

    public override string ToString()
    {
        return Value.ToString(CultureInfo.InvariantCulture);
    }
}
