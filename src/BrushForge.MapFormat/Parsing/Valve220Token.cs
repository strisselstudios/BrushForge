namespace BrushForge.MapFormat.Parsing;

/// <summary>
/// One lexical token with its one-based source line and column.
/// </summary>
public readonly record struct Valve220Token(
    Valve220TokenKind Kind,
    string Text,
    int Line,
    int Column);
