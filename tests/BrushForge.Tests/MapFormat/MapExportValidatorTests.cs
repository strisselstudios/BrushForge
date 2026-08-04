using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.MapFormat.Model;
using BrushForge.MapFormat.Validation;

using BrushForge.Tests.Geometry;

namespace BrushForge.Tests.MapFormat;

public sealed class MapExportValidatorTests
{
    [Fact]
    public void EmptyDocumentIsRejected()
    {
        MapExportValidationResult result =
            MapExportValidator.Validate(
                new MapDocument());

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code ==
                MapExportDiagnosticCodes.NoEntities);
    }

    [Fact]
    public void DocumentWithoutWorldspawnIsRejected()
    {
        MapDocument document = new(
        [
            CreatePointEntity("light")
        ]);

        MapExportValidationResult result =
            MapExportValidator.Validate(document);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code ==
                MapExportDiagnosticCodes.MissingWorldspawn);
    }

    [Fact]
    public void MultipleWorldspawnsAreRejected()
    {
        MapDocument document = new(
        [
            MapEntity.CreateWorldspawn(),
            MapEntity.CreateWorldspawn()
        ]);

        MapExportValidationResult result =
            MapExportValidator.Validate(document);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code ==
                MapExportDiagnosticCodes.MultipleWorldspawns);
    }

    [Fact]
    public void WorldspawnMustBeFirst()
    {
        MapDocument document = new(
        [
            CreatePointEntity("light"),
            MapEntity.CreateWorldspawn()
        ]);

        MapExportValidationResult result =
            MapExportValidator.Validate(document);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code ==
                MapExportDiagnosticCodes.WorldspawnNotFirst);
    }

    [Fact]
    public void EntityWithoutClassnameIsRejected()
    {
        MapDocument document = new(
        [
            MapEntity.CreateWorldspawn(),
            new MapEntity(
            [
                new MapProperty(
                    "origin",
                    "0 0 64")
            ])
        ]);

        MapExportValidationResult result =
            MapExportValidator.Validate(document);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code ==
                MapExportDiagnosticCodes.MissingClassname);
    }

    [Fact]
    public void OpenBrushIsRejected()
    {
        ConvexBrush complete =
            TestBrushFactory.CreateBox(
                Vector3d.Zero,
                new Vector3d(
                    64.0,
                    64.0,
                    64.0));

        ConvexBrush open =
            new(
                complete.Faces.Take(5));

        MapDocument document =
            MapDocument.CreateWorldspawnOnly(
            [
                open
            ]);

        MapExportValidationResult result =
            MapExportValidator.Validate(document);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code ==
                MapExportDiagnosticCodes.InvalidBrush);
    }

    [Fact]
    public void ValidWorldspawnDocumentPasses()
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

        MapExportValidationResult result =
            MapExportValidator.Validate(document);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ValidBrushEntityPasses()
    {
        MapEntity brushEntity = new(
        [
            new MapProperty(
                "classname",
                "func_detail")
        ],
        [
            TestBrushFactory.CreateBox(
                Vector3d.Zero,
                new Vector3d(
                    32.0,
                    32.0,
                    32.0))
        ]);

        MapDocument document = new(
        [
            MapEntity.CreateWorldspawn(),
            brushEntity
        ]);

        MapExportValidationResult result =
            MapExportValidator.Validate(document);

        Assert.True(result.IsValid);
    }

    private static MapEntity CreatePointEntity(
        string classname)
    {
        return new MapEntity(
        [
            new MapProperty(
                "classname",
                classname)
        ]);
    }
}
