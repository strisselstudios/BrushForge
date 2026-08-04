using System.Numerics;
using BrushForge.Core.Numerics;

namespace BrushForge.Core.Randomness;

/// <summary>
/// Stable xoshiro256** generator initialized through SplitMix64.
///
/// This implementation is owned by BrushForge so generated geometry does not
/// change when the runtime implementation of System.Random changes.
/// </summary>
public sealed class DeterministicRandom
{
    private ulong _state0;
    private ulong _state1;
    private ulong _state2;
    private ulong _state3;

    public DeterministicRandom(GenerationSeed seed)
        : this(seed.Value)
    {
    }

    public DeterministicRandom(ulong seed)
    {
        ulong splitMixState = seed;

        _state0 = NextSplitMix64(ref splitMixState);
        _state1 = NextSplitMix64(ref splitMixState);
        _state2 = NextSplitMix64(ref splitMixState);
        _state3 = NextSplitMix64(ref splitMixState);

        if (
            _state0 == 0 &&
            _state1 == 0 &&
            _state2 == 0 &&
            _state3 == 0
        ) {
            _state0 = 0x9E3779B97F4A7C15;
        }
    }

    public ulong NextUInt64()
    {
        unchecked {
            ulong result =
                BitOperations.RotateLeft(_state1 * 5, 7) * 9;

            ulong shifted = _state1 << 17;

            _state2 ^= _state0;
            _state3 ^= _state1;
            _state1 ^= _state2;
            _state0 ^= _state3;
            _state2 ^= shifted;
            _state3 = BitOperations.RotateLeft(_state3, 45);

            return result;
        }
    }

    public uint NextUInt32()
    {
        return (uint)(NextUInt64() >> 32);
    }

    public bool NextBoolean()
    {
        return (NextUInt64() & 1UL) != 0;
    }

    public double NextDouble()
    {
        const double inverseTwoToThe53 = 1.0 / 9007199254740992.0;

        return (NextUInt64() >> 11) * inverseTwoToThe53;
    }

    public double NextDouble(
        double inclusiveMinimum,
        double exclusiveMaximum)
    {
        if (
            !NumericTolerances.IsFinite(inclusiveMinimum) ||
            !NumericTolerances.IsFinite(exclusiveMaximum)
        ) {
            throw new ArgumentOutOfRangeException(
                nameof(exclusiveMaximum),
                "Random-number bounds must be finite.");
        }

        if (exclusiveMaximum <= inclusiveMinimum) {
            throw new ArgumentOutOfRangeException(
                nameof(exclusiveMaximum),
                exclusiveMaximum,
                "The exclusive maximum must exceed the inclusive minimum.");
        }

        double range = exclusiveMaximum - inclusiveMinimum;
        double result = inclusiveMinimum + (NextDouble() * range);

        if (result >= exclusiveMaximum) {
            return Math.BitDecrement(exclusiveMaximum);
        }

        return result;
    }

    public int NextInt32(int exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(exclusiveMaximum),
                exclusiveMaximum,
                "The exclusive maximum must be greater than zero.");
        }

        return (int)NextUInt32Bounded((uint)exclusiveMaximum);
    }

    public int NextInt32(
        int inclusiveMinimum,
        int exclusiveMaximum)
    {
        long range =
            (long)exclusiveMaximum -
            inclusiveMinimum;

        if (range <= 0) {
            throw new ArgumentOutOfRangeException(
                nameof(exclusiveMaximum),
                exclusiveMaximum,
                "The exclusive maximum must exceed the inclusive minimum.");
        }

        uint offset = NextUInt32Bounded((uint)range);

        return checked(
            (int)(inclusiveMinimum + (long)offset));
    }

    private uint NextUInt32Bounded(uint bound)
    {
        if (bound == 0) {
            throw new ArgumentOutOfRangeException(
                nameof(bound),
                bound,
                "The random bound must be greater than zero.");
        }

        uint threshold = unchecked(0U - bound) % bound;

        while (true) {
            uint candidate = NextUInt32();

            if (candidate >= threshold) {
                return candidate % bound;
            }
        }
    }

    private static ulong NextSplitMix64(ref ulong state)
    {
        unchecked {
            state += 0x9E3779B97F4A7C15;

            ulong value = state;

            value =
                (value ^ (value >> 30)) *
                0xBF58476D1CE4E5B9;

            value =
                (value ^ (value >> 27)) *
                0x94D049BB133111EB;

            return value ^ (value >> 31);
        }
    }
}
