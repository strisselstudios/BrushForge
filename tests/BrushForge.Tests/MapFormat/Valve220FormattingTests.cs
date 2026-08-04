using System.Globalization;
using BrushForge.MapFormat.Serialization;

namespace BrushForge.Tests.MapFormat;

public sealed class Valve220FormattingTests
{
    [Fact]
    public void IntegerFormattingHasNoDecimalSuffix()
    {
        Assert.Equal(
            "64",
            Valve220NumberFormatter.Format(
                64.0));
    }

    [Fact]
    public void DecimalFormattingUsesPeriod()
    {
        Assert.Equal(
            "0.5",
            Valve220NumberFormatter.Format(
                0.5));
    }

    [Fact]
    public void NegativeZeroBecomesCanonicalZero()
    {
        Assert.Equal(
            "0",
            Valve220NumberFormatter.Format(
                -0.0));
    }

    [Fact]
    public void NumberFormattingIgnoresCurrentCulture()
    {
        CultureInfo originalCulture =
            CultureInfo.CurrentCulture;

        try {
            CultureInfo.CurrentCulture =
                CultureInfo.GetCultureInfo(
                    "de-DE");

            Assert.Equal(
                "12.25",
                Valve220NumberFormatter.Format(
                    12.25));
        }
        finally {
            CultureInfo.CurrentCulture =
                originalCulture;
        }
    }

    [Fact]
    public void NonFiniteNumbersAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                Valve220NumberFormatter.Format(
                    double.NaN));
    }

    [Fact]
    public void StringCodecEscapesQuotesAndBackslashes()
    {
        string escaped =
            Valve220StringCodec.Escape(
                "C:\\Textures\\\"STONE\"");

        Assert.Equal(
            "C:\\\\Textures\\\\\\\"STONE\\\"",
            escaped);
    }

    [Fact]
    public void StringCodecRejectsLineBreaks()
    {
        Assert.Throws<ArgumentException>(
            () =>
                Valve220StringCodec.Escape(
                    "first\r\nsecond"));
    }
}
