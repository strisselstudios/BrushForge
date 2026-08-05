using System.Text;
using BrushForge.Geometry.Validation;
using BrushForge.Geometry.Vectors;
using BrushForge.MapFormat.Model;
using BrushForge.MapFormat.Parsing;
using BrushForge.MapFormat.Serialization;
using BrushForge.Tests.Geometry;

namespace BrushForge.Tests.MapFormat;

public sealed class Valve220MapReaderTests
{
    [Fact]
    public void ParsesMinimalWorldspawn()
    {
        const string source =
            "{\n" +
            "\"classname\" \"worldspawn\"\n" +
            "}\n";

        MapDocument document =
            Valve220MapReader.Parse(source);

        Assert.Single(document.Entities);
        Assert.True(
            document.Entities[0].IsWorldspawn);
    }

    [Fact]
    public void ParsesPointEntityProperties()
    {
        const string source =
            "{\n" +
            "\"classname\" \"worldspawn\"\n" +
            "}\n" +
            "{\n" +
            "\"classname\" \"light\"\n" +
            "\"origin\" \"0 0 64\"\n" +
            "\"light\" \"300\"\n" +
            "}\n";

        MapDocument document =
            Valve220MapReader.Parse(source);

        Assert.Equal(
            2,
            document.Entities.Count);

        Assert.Equal(
            "light",
            document.Entities[1].ClassName);

        Assert.True(
            document.Entities[1].TryGetProperty(
                "origin",
                out string? origin));

        Assert.Equal(
            "0 0 64",
            origin);
    }

    [Fact]
    public void ParsesCanonicalBox()
    {
        MapDocument original =
            MapDocument.CreateWorldspawnOnly(
            [
                TestBrushFactory.CreateBox(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        64.0,
                        64.0))
            ]);

        string source =
            Valve220MapWriter.Serialize(
                original);

        MapDocument parsed =
            Valve220MapReader.Parse(source);

        Assert.Single(
            parsed.Worldspawn!.Brushes);

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(
                parsed.Worldspawn.Brushes[0]);

        Assert.True(validation.IsValid);
    }

    [Fact]
    public void ParsesKnownTrenchBroomBoxWinding()
    {
        const string source =
            "{\n" +
            "\"classname\" \"worldspawn\"\n" +
            "{\n" +
            "( 0 0 0 ) ( 64 0 0 ) ( 0 64 0 ) STONE [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1\n" +
            "( 0 0 64 ) ( 0 64 64 ) ( 64 0 64 ) STONE [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1\n" +
            "( 0 0 0 ) ( 0 64 0 ) ( 0 0 64 ) STONE [ 0 1 0 0 ] [ 0 0 -1 0 ] 0 1 1\n" +
            "( 64 0 0 ) ( 64 0 64 ) ( 64 64 0 ) STONE [ 0 1 0 0 ] [ 0 0 -1 0 ] 0 1 1\n" +
            "( 0 0 0 ) ( 0 0 64 ) ( 64 0 0 ) STONE [ 1 0 0 0 ] [ 0 0 -1 0 ] 0 1 1\n" +
            "( 0 64 0 ) ( 64 64 0 ) ( 0 64 64 ) STONE [ 1 0 0 0 ] [ 0 0 -1 0 ] 0 1 1\n" +
            "}\n" +
            "}\n";

        MapDocument document =
            Valve220MapReader.Parse(source);

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(
                document.Worldspawn!.Brushes[0]);

        Assert.True(validation.IsValid);
        Assert.Equal(
            Vector3d.Zero,
            validation.Geometry!.Bounds.Minimum);
        Assert.Equal(
            new Vector3d(
                64.0,
                64.0,
                64.0),
            validation.Geometry.Bounds.Maximum);
    }

    [Fact]
    public void PreservesDuplicateProperties()
    {
        const string source =
            "{\n" +
            "\"classname\" \"worldspawn\"\n" +
            "\"target\" \"first\"\n" +
            "\"target\" \"second\"\n" +
            "}\n";

        MapDocument document =
            Valve220MapReader.Parse(source);

        Assert.Equal(
            ["first", "second"],
            document.Worldspawn!
                .GetPropertyValues(
                    "target"));
    }

    [Fact]
    public void ParsesCommentsBetweenTokens()
    {
        const string source =
            "// file comment\n" +
            "{ // entity\n" +
            "\"classname\" // key comment\n" +
            "\"worldspawn\"\n" +
            "}\n";

        MapDocument document =
            Valve220MapReader.Parse(source);

        Assert.True(
            document.Worldspawn!.IsWorldspawn);
    }

    [Fact]
    public void ParsesEscapedStringValues()
    {
        const string source =
            "{\n" +
            "\"classname\" \"worldspawn\"\n" +
            "\"message\" \"A\\\\B\\\"C\"\n" +
            "}\n";

        MapDocument document =
            Valve220MapReader.Parse(source);

        Assert.True(
            document.Worldspawn!.TryGetProperty(
                "message",
                out string? message));

        Assert.Equal(
            "A\\B\"C",
            message);
    }

    [Fact]
    public void ParsesTransparentTextureName()
    {
        MapDocument original =
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

        string source =
            Valve220MapWriter.Serialize(
                original);

        MapDocument parsed =
            Valve220MapReader.Parse(source);

        Assert.All(
            parsed.Worldspawn!.Brushes[0].Faces,
            face =>
                Assert.Equal(
                    "{GLASS",
                    face.TextureName));
    }

    [Fact]
    public void ParsesScientificNotation()
    {
        MapDocument original =
            MapDocument.CreateWorldspawnOnly(
            [
                TestBrushFactory.CreateBox(
                    Vector3d.Zero,
                    new Vector3d(
                        64.0,
                        64.0,
                        64.0))
            ]);

        string source =
            Valve220MapWriter
                .Serialize(original)
                .Replace(
                    "64",
                    "6.4e1",
                    StringComparison.Ordinal);

        MapDocument parsed =
            Valve220MapReader.Parse(source);

        BrushValidationResult validation =
            ConvexBrushValidator.Validate(
                parsed.Worldspawn!.Brushes[0]);

        Assert.True(validation.IsValid);
    }

    [Fact]
    public void RejectsNonNumericCoordinate()
    {
        const string source =
            "{\n" +
            "\"classname\" \"worldspawn\"\n" +
            "{\n" +
            "( nope 0 0 ) ( 0 1 0 ) ( 1 0 0 ) STONE [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1\n" +
            "}\n" +
            "}\n";

        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(
                () =>
                    Valve220MapReader.Parse(
                        source));

        Assert.Contains(
            "not a finite invariant number",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsMissingClosingEntityBrace()
    {
        const string source =
            "{\n" +
            "\"classname\" \"worldspawn\"\n";

        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(
                () =>
                    Valve220MapReader.Parse(
                        source));

        Assert.Contains(
            "missing its closing brace",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsStandardTextureProjectionSyntax()
    {
        const string source =
            "{\n" +
            "\"classname\" \"worldspawn\"\n" +
            "{\n" +
            "( 0 0 0 ) ( 0 1 0 ) ( 1 0 0 ) STONE 0 0 0 1 1\n" +
            "}\n" +
            "}\n";

        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(
                () =>
                    Valve220MapReader.Parse(
                        source));

        Assert.Contains(
            "opening texture-axis bracket",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsBrushWithTooFewFaces()
    {
        const string source =
            "{\n" +
            "\"classname\" \"worldspawn\"\n" +
            "{\n" +
            "( 0 0 0 ) ( 0 1 0 ) ( 1 0 0 ) STONE [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1\n" +
            "( 0 0 0 ) ( 1 0 0 ) ( 0 0 1 ) STONE [ 1 0 0 0 ] [ 0 0 -1 0 ] 0 1 1\n" +
            "( 0 0 0 ) ( 0 0 1 ) ( 0 1 0 ) STONE [ 0 1 0 0 ] [ 0 0 -1 0 ] 0 1 1\n" +
            "}\n" +
            "}\n";

        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(
                () =>
                    Valve220MapReader.Parse(
                        source));

        Assert.Contains(
            "structurally invalid",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnexpectedTopLevelToken()
    {
        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(
                () =>
                    Valve220MapReader.Parse(
                        "\"classname\" \"worldspawn\""));

        Assert.Contains(
            "opening entity brace",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FileReaderAcceptsUtf8Bom()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                "BrushForge.Tests",
                Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(
            directory);

        string path =
            Path.Combine(
                directory,
                "bom.map");

        try {
            const string source =
                "{\n" +
                "\"classname\" \"worldspawn\"\n" +
                "}\n";

            File.WriteAllText(
                path,
                source,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: true));

            MapDocument document =
                Valve220MapReader.ParseFile(
                    path);

            Assert.True(
                document.Worldspawn!.IsWorldspawn);
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
