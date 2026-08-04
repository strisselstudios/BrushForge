using BrushForge.Core.Diagnostics;
using BrushForge.Geometry.Validation;
using BrushForge.MapFormat.Model;

namespace BrushForge.MapFormat.Validation;

/// <summary>
/// Validates document-level requirements and every brush before Valve 220
/// serialization.
/// </summary>
public static class MapExportValidator
{
    public static MapExportValidationResult Validate(
        MapDocument document,
        BrushValidationSettings? brushSettings = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        brushSettings ??=
            BrushValidationSettings.Default;

        DiagnosticBag diagnostics = new();

        if (document.Entities.Count == 0) {
            diagnostics.Add(
                MapExportDiagnosticCodes.NoEntities,
                DiagnosticSeverity.Error,
                "A map document must contain at least one entity.");

            diagnostics.Add(
                MapExportDiagnosticCodes.MissingWorldspawn,
                DiagnosticSeverity.Error,
                "A map document requires one worldspawn entity.");

            return new MapExportValidationResult(
                diagnostics.Snapshot());
        }

        int worldspawnCount =
            document.Entities.Count(
                entity =>
                    entity.IsWorldspawn);

        if (worldspawnCount == 0) {
            diagnostics.Add(
                MapExportDiagnosticCodes.MissingWorldspawn,
                DiagnosticSeverity.Error,
                "A map document requires one worldspawn entity.");
        }
        else if (worldspawnCount > 1) {
            diagnostics.Add(
                MapExportDiagnosticCodes.MultipleWorldspawns,
                DiagnosticSeverity.Error,
                $"A map document contains {worldspawnCount} worldspawn entities.");
        }

        if (
            worldspawnCount > 0 &&
            !document.Entities[0].IsWorldspawn
        ) {
            diagnostics.Add(
                MapExportDiagnosticCodes.WorldspawnNotFirst,
                DiagnosticSeverity.Error,
                "The worldspawn entity must be the first entity in the map document.");
        }

        for (
            int entityIndex = 0;
            entityIndex < document.Entities.Count;
            entityIndex++
        ) {
            MapEntity entity =
                document.Entities[entityIndex];

            if (
                string.IsNullOrWhiteSpace(
                    entity.ClassName)
            ) {
                diagnostics.Add(
                    MapExportDiagnosticCodes.MissingClassname,
                    DiagnosticSeverity.Error,
                    $"Entity {entityIndex} has no classname property.");
            }

            for (
                int brushIndex = 0;
                brushIndex < entity.Brushes.Count;
                brushIndex++
            ) {
                BrushValidationResult brushResult =
                    ConvexBrushValidator.Validate(
                        entity.Brushes[brushIndex],
                        brushSettings);

                if (!brushResult.IsValid) {
                    string details =
                        string.Join(
                            "; ",
                            brushResult.Diagnostics.Select(
                                diagnostic =>
                                    $"{diagnostic.Code}: {diagnostic.Message}"));

                    diagnostics.Add(
                        MapExportDiagnosticCodes.InvalidBrush,
                        DiagnosticSeverity.Error,
                        $"Entity {entityIndex}, brush {brushIndex} is invalid. {details}");
                }
            }
        }

        return new MapExportValidationResult(
            diagnostics.Snapshot());
    }
}
