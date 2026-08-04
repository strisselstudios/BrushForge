using System.Globalization;
using System.Text;
using BrushForge.Geometry.Vectors;
using BrushForge.MapFormat.Model;
using BrushForge.MapFormat.Serialization;

namespace BrushForge.Tests.MapFormat;

public sealed class Valve220MapWriterTests
{
    [Fact]
    public void MinimalWorldspawnHasCanonicalStructure()
    {
        MapDocument document =
            MapDocument.CreateWorldspawnOnly();

        string serialized =
            Valve220MapWriter.Serialize(document);

        Assert.Equal(
            "{\r\n" +
            "\"classname\" \"worldspawn\"\r\n" +
            "}\r\n",
            serialized);
    }

    [Fact]
    public void BoxFaceUsesValve220Syntax()
    {
        MapDocument document =
            MapDocument.CreateWorldspawnOnly(
            [
                TestBrushFactory.CreateBox(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        64.0,
                        64.0))
            ]);

        string serialized =
            Valve220MapWriter.Serialize(document);

        Assert.Contains(
            "( 0 0 0 ) ( 0 64 0 ) ( 64 0 0 ) STONE [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1\r\n",
            serialized);
    }

    [Fact]
    public void EntityAndPropertyOrderIsPreserved()
    {
        MapDocument document = new(
        [
            MapEntity.CreateWorldspawn(
                additionalProperties:
                [
                    new MapProperty(
                        "message",
                        "First"),
                    new MapProperty(
                        "wad",
                        "textures.wad")
                ]),
            new MapEntity(
            [
                new MapProperty(
                    "classname",
                    "light"),
                new MapProperty(
                    "origin",
                    "0 0 64")
            ])
        ]);

        string serialized =
            Valve220MapWriter.Serialize(document);

        int messageIndex =
            serialized.IndexOf(
                "\"message\" \"First\"",
                StringComparison.Ordinal);

        int wadIndex =
            serialized.IndexOf(
                "\"wad\" \"textures.wad\"",
                StringComparison.Ordinal);

        int lightIndex =
            serialized.IndexOf(
                "\"classname\" \"light\"",
                StringComparison.Ordinal);

        Assert.True(messageIndex > 0);
        Assert.True(wadIndex > messageIndex);
        Assert.True(lightIndex > wadIndex);
    }

    [Fact]
    public void WriterEscapesQuotedPropertyValues()
    {
        MapDocument document =
            MapDocument.CreateWorldspawnOnly(
                additionalProperties:
                [
                    new MapProperty(
                        "message",
                        "Brush \"Forge\"")
                ]);

        string serialized =
            Valve220MapWriter.Serialize(document);

        Assert.Contains(
            "\"message\" \"Brush \\\"Forge\\\"\"",
            serialized);
    }

    [Fact]
    public void WriterIsIndependentOfCurrentCulture()
    {
        CultureInfo originalCulture =
            CultureInfo.CurrentCulture;

        try {
            CultureInfo.CurrentCulture =
                CultureInfo.GetCultureInfo(
                    "fr-FR");

            MapDocument document =
                MapDocument.CreateWorldspawnOnly(
                [
                    TestBrushFactory.CreateBox(
                        Vector3d.Zero,
                        new Vector3d(
                            12.5,
                            16.25,
                            32.75))
                ]);

            string serialized =
                Valve220MapWriter.Serialize(document);

            Assert.Contains(
                "12.5",
                serialized);

            Assert.DoesNotContain(
                "12,5",
                serialized);
        }
        finally {
            CultureInfo.CurrentCulture =
                originalCulture;
        }
    }

    [Fact]
    public void InvalidDocumentIsRejectedBeforeWriting()
    {
        MapDocument document = new();

        Assert.Throws<InvalidOperationException>(
            () =>
                Valve220MapWriter.Serialize(
                    document));
    }

    [Fact]
    public void ValidationCanBeDisabledForDiagnosticOutput()
    {
        MapDocument document = new();

        string serialized =
            Valve220MapWriter.Serialize(
                document,
                new Valve220WriteOptions(
                    validateBeforeWriting: false));

        Assert.Equal(
            string.Empty,
            serialized);
    }

    [Fact]
    public void FileOutputUsesUtf8WithoutBom()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                "BrushForge.Tests",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        string path =
            Path.Combine(
                directory,
                "test.map");

        try {
            Valve220MapWriter.WriteFile(
                path,
                MapDocument.CreateWorldspawnOnly());

            byte[] bytes =
                File.ReadAllBytes(path);

            byte[] preamble =
                Encoding.UTF8.GetPreamble();

            Assert.False(
                bytes.Length >= preamble.Length &&
                bytes
                    .Take(preamble.Length)
                    .SequenceEqual(preamble));
        }
        finally {
            if (Directory.Exists(directory)) {
                Directory.Delete(
                    directory,
                    recursive: true);
            }
        }
    }
}
