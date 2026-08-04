using System.Collections.ObjectModel;
using System.Text;

namespace BrushForge.MapFormat.Parsing;

/// <summary>
/// Tokenizes Valve 220 map text while discarding whitespace and line comments.
/// </summary>
public sealed class Valve220Tokenizer
{
    private readonly string _source;
    private int _index;
    private int _line = 1;
    private int _column = 1;

    public Valve220Tokenizer(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    public IReadOnlyList<Valve220Token> Tokenize()
    {
        List<Valve220Token> tokens = [];

        while (true) {
            Valve220Token token =
                ReadNextToken();

            tokens.Add(token);

            if (token.Kind == Valve220TokenKind.End) {
                break;
            }
        }

        return new ReadOnlyCollection<Valve220Token>(
            tokens);
    }

    private Valve220Token ReadNextToken()
    {
        SkipTrivia();

        if (IsAtEnd) {
            return new Valve220Token(
                Valve220TokenKind.End,
                string.Empty,
                _line,
                _column);
        }

        int tokenLine = _line;
        int tokenColumn = _column;
        char character = Current;

        if (
            character == '{' &&
            IsTransparentTextureStart()
        ) {
            return ReadBareToken(
                tokenLine,
                tokenColumn,
                includeLeadingBrace: true);
        }

        switch (character) {
            case '{':
                Advance();

                return new Valve220Token(
                    Valve220TokenKind.OpenBrace,
                    "{",
                    tokenLine,
                    tokenColumn);

            case '}':
                Advance();

                return new Valve220Token(
                    Valve220TokenKind.CloseBrace,
                    "}",
                    tokenLine,
                    tokenColumn);

            case '(':
                Advance();

                return new Valve220Token(
                    Valve220TokenKind.OpenParenthesis,
                    "(",
                    tokenLine,
                    tokenColumn);

            case ')':
                Advance();

                return new Valve220Token(
                    Valve220TokenKind.CloseParenthesis,
                    ")",
                    tokenLine,
                    tokenColumn);

            case '[':
                Advance();

                return new Valve220Token(
                    Valve220TokenKind.OpenBracket,
                    "[",
                    tokenLine,
                    tokenColumn);

            case ']':
                Advance();

                return new Valve220Token(
                    Valve220TokenKind.CloseBracket,
                    "]",
                    tokenLine,
                    tokenColumn);

            case '"':
                return ReadQuotedString(
                    tokenLine,
                    tokenColumn);

            default:
                return ReadBareToken(
                    tokenLine,
                    tokenColumn,
                    includeLeadingBrace: false);
        }
    }

    private Valve220Token ReadQuotedString(
        int tokenLine,
        int tokenColumn)
    {
        Advance();

        StringBuilder builder = new();

        while (!IsAtEnd) {
            char character = Current;

            if (character == '"') {
                Advance();

                return new Valve220Token(
                    Valve220TokenKind.QuotedString,
                    builder.ToString(),
                    tokenLine,
                    tokenColumn);
            }

            if (character is '\r' or '\n') {
                throw CreateLexicalError(
                    tokenLine,
                    tokenColumn,
                    "Quoted strings cannot span multiple lines.");
            }

            if (character != '\\') {
                builder.Append(character);
                Advance();
                continue;
            }

            Advance();

            if (IsAtEnd) {
                throw CreateLexicalError(
                    tokenLine,
                    tokenColumn,
                    "A quoted string ends with an incomplete escape sequence.");
            }

            char escapedCharacter =
                Current;

            if (escapedCharacter is '\r' or '\n') {
                throw CreateLexicalError(
                    tokenLine,
                    tokenColumn,
                    "Quoted strings cannot span multiple lines.");
            }

            if (escapedCharacter is '\\' or '"') {
                builder.Append(
                    escapedCharacter);
            }
            else {
                builder.Append('\\');
                builder.Append(
                    escapedCharacter);
            }

            Advance();
        }

        throw CreateLexicalError(
            tokenLine,
            tokenColumn,
            "The quoted string is not terminated.");
    }

    private Valve220Token ReadBareToken(
        int tokenLine,
        int tokenColumn,
        bool includeLeadingBrace)
    {
        int startIndex = _index;

        if (includeLeadingBrace) {
            Advance();
        }

        while (
            !IsAtEnd &&
            !char.IsWhiteSpace(Current) &&
            !IsDelimiter(Current) &&
            !IsLineCommentStart()
        ) {
            Advance();
        }

        string text =
            _source[startIndex.._index];

        if (text.Length == 0) {
            throw CreateLexicalError(
                tokenLine,
                tokenColumn,
                $"Unexpected character '{Current}'.");
        }

        return new Valve220Token(
            Valve220TokenKind.Bare,
            text,
            tokenLine,
            tokenColumn);
    }

    private void SkipTrivia()
    {
        while (true) {
            while (
                !IsAtEnd &&
                char.IsWhiteSpace(Current)
            ) {
                Advance();
            }

            if (!IsLineCommentStart()) {
                return;
            }

            Advance();
            Advance();

            while (
                !IsAtEnd &&
                Current is not '\r' and not '\n'
            ) {
                Advance();
            }
        }
    }

    private bool IsTransparentTextureStart()
    {
        if (
            Current != '{' ||
            _index + 1 >= _source.Length
        ) {
            return false;
        }

        char next =
            _source[_index + 1];

        return
            !char.IsWhiteSpace(next) &&
            next is not
                '{' and not
                '}' and not
                '(' and not
                ')' and not
                '[' and not
                ']' and not
                '"' and not
                '/';
    }

    private bool IsLineCommentStart()
    {
        return
            !IsAtEnd &&
            Current == '/' &&
            _index + 1 < _source.Length &&
            _source[_index + 1] == '/';
    }

    private static bool IsDelimiter(
        char character)
    {
        return character is
            '{' or
            '}' or
            '(' or
            ')' or
            '[' or
            ']' or
            '"';
    }

    private bool IsAtEnd =>
        _index >= _source.Length;

    private char Current =>
        _source[_index];

    private void Advance()
    {
        if (IsAtEnd) {
            return;
        }

        char character =
            _source[_index];

        _index++;

        if (character == '\r') {
            if (
                _index < _source.Length &&
                _source[_index] == '\n'
            ) {
                _index++;
            }

            _line++;
            _column = 1;
            return;
        }

        if (character == '\n') {
            _line++;
            _column = 1;
            return;
        }

        _column++;
    }

    private static InvalidDataException CreateLexicalError(
        int line,
        int column,
        string message)
    {
        return new InvalidDataException(
            $"Valve 220 lexical error at line {line}, column {column}: {message}");
    }
}
