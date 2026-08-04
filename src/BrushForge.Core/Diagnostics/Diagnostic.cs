namespace BrushForge.Core.Diagnostics;

/// <summary>
/// A structured diagnostic suitable for validation, generation, and export.
/// </summary>
public sealed record Diagnostic
{
    public Diagnostic(
        string code,
        DiagnosticSeverity severity,
        string message)
    {
        if (string.IsNullOrWhiteSpace(code)) {
            throw new ArgumentException(
                "A diagnostic code is required.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message)) {
            throw new ArgumentException(
                "A diagnostic message is required.",
                nameof(message));
        }

        Code = code.Trim();
        Severity = severity;
        Message = message.Trim();
    }

    public string Code { get; }

    public DiagnosticSeverity Severity { get; }

    public string Message { get; }
}
