using BrushForge.Core.Randomness;

namespace BrushForge.Tests.Core;

public sealed class DeterministicRandomTests
{
    [Fact]
    public void TextSeedHashIsStable()
    {
        GenerationSeed seed =
            GenerationSeed.FromText("BrushForge");

        Assert.Equal(
            11817673973175158674UL,
            seed.Value);
    }

    [Fact]
    public void GeneratorSequenceIsStable()
    {
        DeterministicRandom random = new(
            GenerationSeed.FromText("BrushForge"));

        Assert.Equal(
            10592668833978081119UL,
            random.NextUInt64());

        Assert.Equal(
            15068346368323096312UL,
            random.NextUInt64());

        Assert.Equal(
            14205090997539022814UL,
            random.NextUInt64());

        Assert.Equal(
            12838235242544164770UL,
            random.NextUInt64());

        Assert.Equal(
            10484591820209412926UL,
            random.NextUInt64());
    }

    [Fact]
    public void EqualSeedsProduceEqualSequences()
    {
        DeterministicRandom first = new(123456789UL);
        DeterministicRandom second = new(123456789UL);

        for (int index = 0; index < 256; index++) {
            Assert.Equal(
                first.NextUInt64(),
                second.NextUInt64());
        }
    }

    [Fact]
    public void IntegerRangeHonorsInclusiveAndExclusiveBounds()
    {
        DeterministicRandom random = new(987654321UL);

        for (int index = 0; index < 1000; index++) {
            int value = random.NextInt32(-25, 40);

            Assert.InRange(value, -25, 39);
        }
    }

    [Fact]
    public void UnitDoubleNeverReachesOne()
    {
        DeterministicRandom random = new(11223344UL);

        for (int index = 0; index < 1000; index++) {
            double value = random.NextDouble();

            Assert.True(value >= 0.0);
            Assert.True(value < 1.0);
        }
    }

    [Fact]
    public void SeedCanRoundTripThroughInvariantText()
    {
        GenerationSeed original = new(ulong.MaxValue);
        string text = original.ToString();
        GenerationSeed parsed = GenerationSeed.Parse(text);

        Assert.Equal(original, parsed);
    }
}
