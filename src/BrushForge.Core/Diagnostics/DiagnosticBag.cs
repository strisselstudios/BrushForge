namespace BrushForge.Core.Diagnostics;

/// <summary>
/// Mutable collector used while an operation is running. Call Snapshot to
/// obtain an isolated immutable-by-convention result array.
/// </summary>
public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = [];

    public int Count => _diagnostics.Count;

    public bool HasErrors =>
        _diagnostics.Any(
            diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error);

    public bool HasWarnings =>
        _diagnostics.Any(
            diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Warning);

    public void Add(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        _diagnostics.Add(diagnostic);
    }

    public void Add(
        string code,
        DiagnosticSeverity severity,
        string message)
    {
        Add(new Diagnostic(code, severity, message));
    }

    public IReadOnlyList<Diagnostic> Snapshot()
    {
        return _diagnostics.ToArray();
    }

    public void Clear()
    {
        _diagnostics.Clear();
    }
}
