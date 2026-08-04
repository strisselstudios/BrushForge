using BrushForge.MapFormat.Parsing;

namespace BrushForge.Tests.MapFormat;

public sealed class Valve220TokenizerTests
{
    [Fact]
    public void TokenizesCanonicalSymbolsAndBareTokens()
    {
        const string source =
            "{ ( 0 0 0 ) [ 1 0 0 0 ] }";

        Valve220TokenKind[] kinds =
            new Valve220Tokenizer(source)
                .Tokenize()
                .Select(
                    token =>
                        token.Kind)
                .ToArray();

        Assert.Equal(
        [
            Valve220TokenKind.OpenBrace,
            Valve220TokenKind.OpenParenthesis,
            Valve220TokenKind.Bare,
            Valve220TokenKind.Bare,
            Valve220TokenKind.Bare,
            Valve220TokenKind.CloseParenthesis,
            Valve220TokenKind.OpenBracket,
            Valve220TokenKind.Bare,
            Valve220TokenKind.Bare,
            Valve220TokenKind.Bare,
            Valve220TokenKind.Bare,
            Valve220TokenKind.CloseBracket,
            Valve220TokenKind.CloseBrace,
            Valve220TokenKind.End
        ],
            kinds);
    }

    [Fact]
    public void SkipsLineComments()
    {
        const string source =
            "// first comment\n{\n// second comment\n}\n";

        Valve220Token[] tokens =
            new Valve220Tokenizer(source)
                .Tokenize()
                .ToArray();

        Assert.Equal(3, tokens.Length);
        Assert.Equal(
            Valve220TokenKind.OpenBrace,
            tokens[0].Kind);

        Assert.Equal(
            Valve220TokenKind.CloseBrace,
            tokens[1].Kind);

        Assert.Equal(
            Valve220TokenKind.End,
            tokens[2].Kind);
    }

    [Fact]
    public void DecodesQuotedEscapes()
    {
        const string source =
            "\"A\\\\B\\\"C\"";

        Valve220Token token =
            new Valve220Tokenizer(source)
                .Tokenize()[0];

        Assert.Equal(
            "A\\B\"C",
            token.Text);
    }

    [Fact]
    public void PreservesUnknownBackslashEscapes()
    {
        const string source =
            "\"A\\qB\"";

        Valve220Token token =
            new Valve220Tokenizer(source)
                .Tokenize()[0];

        Assert.Equal(
            "A\\qB",
            token.Text);
    }

    [Fact]
    public void ReportsOneBasedLineAndColumn()
    {
        const string source =
            "// comment\r\n{\r\n\"key\" \"value\"\r\n}";

        Valve220Token[] tokens =
            new Valve220Tokenizer(source)
                .Tokenize()
                .ToArray();

        Assert.Equal(2, tokens[0].Line);
        Assert.Equal(1, tokens[0].Column);
        Assert.Equal(3, tokens[1].Line);
        Assert.Equal(1, tokens[1].Column);
    }

    [Fact]
    public void TransparentTextureIsOneBareToken()
    {
        const string source =
            "{GLASS";

        Valve220Token[] tokens =
            new Valve220Tokenizer(source)
                .Tokenize()
                .ToArray();

        Assert.Equal(
            Valve220TokenKind.Bare,
            tokens[0].Kind);

        Assert.Equal(
            "{GLASS",
            tokens[0].Text);
    }

    [Fact]
    public void UnterminatedQuotedStringIsRejected()
    {
        InvalidDataException exception =
            Assert.Throws<InvalidDataException>(
                () =>
                    new Valve220Tokenizer(
                        "\"unterminated")
                        .Tokenize());

        Assert.Contains(
            "line 1, column 1",
            exception.Message,
            StringComparison.Ordinal);
    }
}
