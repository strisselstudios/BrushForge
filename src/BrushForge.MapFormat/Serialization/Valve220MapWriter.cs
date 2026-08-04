using System.Globalization;
using System.Text;
using BrushForge.Core.Diagnostics;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Vectors;
using BrushForge.MapFormat.Model;
using BrushForge.MapFormat.Validation;

namespace BrushForge.MapFormat.Serialization;

/// <summary>
/// Writes canonical TrenchBroom-compatible Valve 220 map syntax.
/// </summary>
public static class Valve220MapWriter
{
    private static readonly UTF8Encoding Utf8WithoutBom =
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    public static string Serialize(
        MapDocument document,
        Valve220WriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        options ??=
            Valve220WriteOptions.Default;

        using StringWriter writer =
            new(CultureInfo.InvariantCulture)
            {
                NewLine = options.NewLine
            };

        Write(
            writer,
            document,
            options);

        return writer.ToString();
    }

    public static void Write(
        TextWriter writer,
        MapDocument document,
        Valve220WriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(document);

        options ??=
            Valve220WriteOptions.Default;

        if (options.ValidateBeforeWriting) {
            MapExportValidationResult validation =
                MapExportValidator.Validate(document);

            if (!validation.IsValid) {
                string message =
                    string.Join(
                        Environment.NewLine,
                        validation.Diagnostics
                            .Where(
                                diagnostic =>
                                    diagnostic.Severity ==
                                    DiagnosticSeverity.Error)
                            .Select(
                                diagnostic =>
                                    $"{diagnostic.Code}: {diagnostic.Message}"));

                throw new InvalidOperationException(
                    $"The map document failed export validation.{Environment.NewLine}{message}");
            }
        }

        foreach (MapEntity entity in document.Entities) {
            WriteEntity(
                writer,
                entity);
        }
    }

    public static void WriteFile(
        string path,
        MapDocument document,
        Valve220WriteOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(document);

        string content =
            Serialize(
                document,
                options);

        File.WriteAllText(
            path,
            content,
            Utf8WithoutBom);
    }

    private static void WriteEntity(
        TextWriter writer,
        MapEntity entity)
    {
        writer.WriteLine("{");

        foreach (
            MapProperty property
            in entity.Properties
        ) {
            writer.Write('"');
            writer.Write(
                Valve220StringCodec.Escape(
                    property.Key));

            writer.Write("\" \"");

            writer.Write(
                Valve220StringCodec.Escape(
                    property.Value));

            writer.WriteLine('"');
        }

        foreach (
            ConvexBrush brush
            in entity.Brushes
        ) {
            WriteBrush(
                writer,
                brush);
        }

        writer.WriteLine("}");
    }

    private static void WriteBrush(
        TextWriter writer,
        ConvexBrush brush)
    {
        writer.WriteLine("{");

        foreach (
            BrushFace face
            in brush.Faces
        ) {
            WriteFace(
                writer,
                face);
        }

        writer.WriteLine("}");
    }

    private static void WriteFace(
        TextWriter writer,
        BrushFace face)
    {
        WritePoint(
            writer,
            face.PlanePoints.First);

        writer.Write(' ');

        WritePoint(
            writer,
            face.PlanePoints.Second);

        writer.Write(' ');

        WritePoint(
            writer,
            face.PlanePoints.Third);

        writer.Write(' ');
        writer.Write(face.TextureName);

        writer.Write(" [ ");
        WriteVectorComponents(
            writer,
            face.TextureAxes.UAxis.Direction);

        writer.Write(' ');
        writer.Write(
            Valve220NumberFormatter.Format(
                face.TextureAxes.UAxis.Offset));

        writer.Write(" ] [ ");
        WriteVectorComponents(
            writer,
            face.TextureAxes.VAxis.Direction);

        writer.Write(' ');
        writer.Write(
            Valve220NumberFormatter.Format(
                face.TextureAxes.VAxis.Offset));

        writer.Write(" ] ");

        writer.Write(
            Valve220NumberFormatter.Format(
                face.TextureAxes.RotationDegrees));

        writer.Write(' ');

        writer.Write(
            Valve220NumberFormatter.Format(
                face.TextureAxes.UAxis.Scale));

        writer.Write(' ');

        writer.WriteLine(
            Valve220NumberFormatter.Format(
                face.TextureAxes.VAxis.Scale));
    }

    private static void WritePoint(
        TextWriter writer,
        Vector3d point)
    {
        writer.Write("( ");
        WriteVectorComponents(
            writer,
            point);
        writer.Write(" )");
    }

    private static void WriteVectorComponents(
        TextWriter writer,
        Vector3d vector)
    {
        writer.Write(
            Valve220NumberFormatter.Format(
                vector.X));

        writer.Write(' ');

        writer.Write(
            Valve220NumberFormatter.Format(
                vector.Y));

        writer.Write(' ');

        writer.Write(
            Valve220NumberFormatter.Format(
                vector.Z));
    }
}
