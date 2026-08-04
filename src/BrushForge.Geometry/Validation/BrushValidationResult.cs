using System.Collections.ObjectModel;
using BrushForge.Core.Diagnostics;

namespace BrushForge.Geometry.Validation;

public sealed class BrushValidationResult
{
    private readonly ReadOnlyCollection<Diagnostic> _diagnostics;

    public BrushValidationResult(
        ConvexBrushGeometry? geometry,
        IEnumerable<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Diagnostic[] diagnosticArray = diagnostics.ToArray();

        if (diagnosticArray.Any(diagnostic => diagnostic is null)) {
            throw new ArgumentException(
                "Validation diagnostics cannot contain null values.",
                nameof(diagnostics));
        }

        Geometry = geometry;
        _diagnostics = Array.AsReadOnly(diagnosticArray);
    }

    public ConvexBrushGeometry? Geometry { get; }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public bool HasErrors =>
        _diagnostics.Any(
            diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error);

    public bool IsValid =>
        Geometry is not null &&
        Geometry.IsClosed &&
        !HasErrors;
}
