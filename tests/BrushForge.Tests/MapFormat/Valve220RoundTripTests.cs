using BrushForge.Geometry.Vectors;
using BrushForge.MapFormat.Model;
using BrushForge.MapFormat.Parsing;
using BrushForge.MapFormat.Serialization;
using BrushForge.Tests.Geometry;

namespace BrushForge.Tests.MapFormat;

public sealed class Valve220RoundTripTests
{
    [Fact]
    public void MinimalWorldspawnRoundTrips()
    {
        AssertCanonicalRoundTrip(
            MapDocument.CreateWorldspawnOnly());
    }

    [Fact]
    public void BoxRoundTrips()
    {
        AssertCanonicalRoundTrip(
            MapDocument.CreateWorldspawnOnly(
            [
                TestBrushFactory.CreateBox(
                    new Vector3d(
                        -32.0,
                        -16.0,
                        8.0),
                    new Vector3d(
                        32.0,
                        48.0,
                        72.0))
            ]));
    }

    [Fact]
    public void MultipleEntitiesRoundTrip()
    {
        MapDocument document = new(
        [
            MapEntity.CreateWorldspawn(),
            new MapEntity(
            [
                new MapProperty(
                    "classname",
                    "light"),
                new MapProperty(
                    "origin",
                    "0 0 64"),
                new MapProperty(
                    "light",
                    "300")
            ])
        ]);

        AssertCanonicalRoundTrip(
            document);
    }

    [Fact]
    public void EscapedPropertyRoundTrips()
    {
        MapDocument document =
            MapDocument.CreateWorldspawnOnly(
                additionalProperties:
                [
                    new MapProperty(
                        "message",
                        "A\\B\"C")
                ]);

        AssertCanonicalRoundTrip(
            document);
    }

    [Fact]
    public void TransparentTextureRoundTrips()
    {
        MapDocument document =
            MapDocument.CreateWorldspawnOnly(
            [
                TestBrushFactory.CreateBox(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        64.0,
                        64.0),
                    "{GLASS")
            ]);

        AssertCanonicalRoundTrip(
            document);
    }

    private static void AssertCanonicalRoundTrip(
        MapDocument document)
    {
        string first =
            Valve220MapWriter.Serialize(
                document);

        MapDocument parsed =
            Valve220MapReader.Parse(
                first);

        string second =
            Valve220MapWriter.Serialize(
                parsed);

        Assert.Equal(
            first,
            second);
    }
}
