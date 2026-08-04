using System.Collections.ObjectModel;
using BrushForge.Core.Diagnostics;

namespace BrushForge.MapFormat.Validation;

public sealed class MapExportValidationResult
{
    private readonly ReadOnlyCollection<Diagnostic> _diagnostics;

    public MapExportValidationResult(
        IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Diagnostic[] diagnosticArray =
            diagnostics.ToArray();

        if (
            diagnosticArray.Any(
                diagnostic =>
                    diagnostic is null)
        ) {
            throw new ArgumentException(
                "Export diagnostics cannot contain null values.",
                nameof(diagnostics));
        }

        _diagnostics =
            Array.AsReadOnly(diagnosticArray);
    }

    public IReadOnlyList<Diagnostic> Diagnostics =>
        _diagnostics;

    public bool HasErrors =>
        _diagnostics.Any(
            diagnostic =>
                diagnostic.Severity ==
                DiagnosticSeverity.Error);

    public bool IsValid => !HasErrors;
}
