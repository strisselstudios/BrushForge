using System.Globalization;
using System.Text;
using BrushForge.Core.Numerics;
using BrushForge.Geometry.Brushes;
using BrushForge.Geometry.Textures;
using BrushForge.Geometry.Vectors;
using BrushForge.MapFormat.Model;

namespace BrushForge.MapFormat.Parsing;

/// <summary>
/// Parses Valve 220 map text into the BrushForge map model.
/// </summary>
public static class Valve220MapReader
{
    private static readonly UTF8Encoding StrictUtf8 =
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    public static MapDocument Parse(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Valve220Token[] tokens =
            new Valve220Tokenizer(source)
                .Tokenize()
                .ToArray();

        Parser parser = new(tokens);

        return parser.ParseDocument();
    }

    public static MapDocument ParseFile(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        string source =
            File.ReadAllText(
                path,
                StrictUtf8);

        return Parse(source);
    }

    private sealed class Parser
    {
        private readonly Valve220Token[] _tokens;
        private int _index;

        public Parser(
            Valve220Token[] tokens)
        {
            ArgumentNullException.ThrowIfNull(tokens);

            if (tokens.Length == 0) {
                throw new ArgumentException(
                    "The parser requires an end token.",
                    nameof(tokens));
            }

            _tokens = tokens;
        }

        public MapDocument ParseDocument()
        {
            List<MapEntity> entities = [];

            while (
                Current.Kind !=
                Valve220TokenKind.End
            ) {
                entities.Add(
                    ParseEntity());
            }

            return new MapDocument(
                entities);
        }

        private MapEntity ParseEntity()
        {
            Valve220Token openingToken =
                Consume(
                    Valve220TokenKind.OpenBrace,
                    "an opening entity brace");

            List<MapProperty> properties = [];
            List<ConvexBrush> brushes = [];

            while (
                Current.Kind !=
                Valve220TokenKind.CloseBrace
            ) {
                if (
                    Current.Kind ==
                    Valve220TokenKind.End
                ) {
                    throw CreateParseError(
                        Current,
                        "The entity is missing its closing brace.");
                }

                if (
                    Current.Kind ==
                    Valve220TokenKind.QuotedString
                ) {
                    properties.Add(
                        ParseProperty());

                    continue;
                }

                if (
                    Current.Kind ==
                    Valve220TokenKind.OpenBrace
                ) {
                    brushes.Add(
                        ParseBrush());

                    continue;
                }

                throw CreateParseError(
                    Current,
                    "Expected an entity property or brush.");
            }

            Consume(
                Valve220TokenKind.CloseBrace,
                "a closing entity brace");

            try {
                return new MapEntity(
                    properties,
                    brushes);
            }
            catch (ArgumentException exception) {
                throw CreateParseError(
                    openingToken,
                    "The parsed entity is invalid.",
                    exception);
            }
        }

        private MapProperty ParseProperty()
        {
            Valve220Token keyToken =
                Consume(
                    Valve220TokenKind.QuotedString,
                    "a quoted property key");

            Valve220Token valueToken =
                Consume(
                    Valve220TokenKind.QuotedString,
                    "a quoted property value");

            try {
                return new MapProperty(
                    keyToken.Text,
                    valueToken.Text);
            }
            catch (ArgumentException exception) {
                throw CreateParseError(
                    keyToken,
                    "The entity property is invalid.",
                    exception);
            }
        }

        private ConvexBrush ParseBrush()
        {
            Valve220Token openingToken =
                Consume(
                    Valve220TokenKind.OpenBrace,
                    "an opening brush brace");

            List<BrushFace> faces = [];

            while (
                Current.Kind !=
                Valve220TokenKind.CloseBrace
            ) {
                if (
                    Current.Kind ==
                    Valve220TokenKind.End
                ) {
                    throw CreateParseError(
                        Current,
                        "The brush is missing its closing brace.");
                }

                if (
                    Current.Kind !=
                    Valve220TokenKind.OpenParenthesis
                ) {
                    throw CreateParseError(
                        Current,
                        "Expected a Valve 220 brush face.");
                }

                faces.Add(
                    ParseFace());
            }

            Consume(
                Valve220TokenKind.CloseBrace,
                "a closing brush brace");

            try {
                return new ConvexBrush(
                    faces);
            }
            catch (ArgumentException exception) {
                throw CreateParseError(
                    openingToken,
                    "The parsed brush is structurally invalid.",
                    exception);
            }
        }

        private BrushFace ParseFace()
        {
            Valve220Token faceToken =
                Current;

            Vector3d first =
                ParsePoint();

            Vector3d second =
                ParsePoint();

            Vector3d third =
                ParsePoint();

            Valve220Token textureToken =
                Consume(
                    Valve220TokenKind.Bare,
                    "an unquoted texture name");

            TextureAxis uAxis =
                ParseTextureAxis();

            TextureAxis vAxis =
                ParseTextureAxis();

            double rotation =
                ParseNumber(
                    "a texture rotation");

            double uScale =
                ParseNumber(
                    "a U texture scale");

            double vScale =
                ParseNumber(
                    "a V texture scale");

            try {
                PlanePoints3d planePoints =
                    new(
                        first,
                        second,
                        third);

                Valve220TextureAxes axes =
                    new(
                        new TextureAxis(
                            uAxis.Direction,
                            uAxis.Offset,
                            uScale),
                        new TextureAxis(
                            vAxis.Direction,
                            vAxis.Offset,
                            vScale),
                        rotation);

                return new BrushFace(
                    planePoints,
                    textureToken.Text,
                    axes);
            }
            catch (ArgumentException exception) {
                throw CreateParseError(
                    faceToken,
                    "The parsed brush face is invalid.",
                    exception);
            }
            catch (OverflowException exception) {
                throw CreateParseError(
                    faceToken,
                    "The parsed brush face exceeds supported numeric limits.",
                    exception);
            }
            catch (DivideByZeroException exception) {
                throw CreateParseError(
                    faceToken,
                    "The parsed brush face contains an invalid zero divisor.",
                    exception);
            }
        }

        private Vector3d ParsePoint()
        {
            Consume(
                Valve220TokenKind.OpenParenthesis,
                "an opening point parenthesis");

            double x =
                ParseNumber(
                    "an X coordinate");

            double y =
                ParseNumber(
                    "a Y coordinate");

            double z =
                ParseNumber(
                    "a Z coordinate");

            Consume(
                Valve220TokenKind.CloseParenthesis,
                "a closing point parenthesis");

            return new Vector3d(
                x,
                y,
                z);
        }

        private TextureAxis ParseTextureAxis()
        {
            Valve220Token axisToken =
                Consume(
                    Valve220TokenKind.OpenBracket,
                    "an opening texture-axis bracket");

            double x =
                ParseNumber(
                    "a texture-axis X component");

            double y =
                ParseNumber(
                    "a texture-axis Y component");

            double z =
                ParseNumber(
                    "a texture-axis Z component");

            double offset =
                ParseNumber(
                    "a texture-axis offset");

            Consume(
                Valve220TokenKind.CloseBracket,
                "a closing texture-axis bracket");

            try {
                return new TextureAxis(
                    new Vector3d(
                        x,
                        y,
                        z),
                    offset,
                    scale: 1.0);
            }
            catch (ArgumentException exception) {
                throw CreateParseError(
                    axisToken,
                    "The texture axis is invalid.",
                    exception);
            }
        }

        private double ParseNumber(
            string description)
        {
            Valve220Token token =
                Consume(
                    Valve220TokenKind.Bare,
                    description);

            bool parsed =
                double.TryParse(
                    token.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double value);

            if (
                !parsed ||
                !NumericTolerances.IsFinite(value)
            ) {
                throw CreateParseError(
                    token,
                    $"Expected {description}, but '{token.Text}' is not a finite invariant number.");
            }

            return value;
        }

        private Valve220Token Consume(
            Valve220TokenKind expectedKind,
            string description)
        {
            Valve220Token token =
                Current;

            if (token.Kind != expectedKind) {
                throw CreateParseError(
                    token,
                    $"Expected {description}, but found {Describe(token)}.");
            }

            if (
                _index <
                _tokens.Length - 1
            ) {
                _index++;
            }

            return token;
        }

        private Valve220Token Current =>
            _tokens[_index];

        private static string Describe(
            Valve220Token token)
        {
            if (
                token.Kind ==
                Valve220TokenKind.End
            ) {
                return "the end of the file";
            }

            return
                $"{token.Kind} token '{token.Text}'";
        }

        private static InvalidDataException CreateParseError(
            Valve220Token token,
            string message)
        {
            return new InvalidDataException(
                $"Valve 220 parse error at line {token.Line}, column {token.Column}: {message}");
        }

        private static InvalidDataException CreateParseError(
            Valve220Token token,
            string message,
            Exception innerException)
        {
            return new InvalidDataException(
                $"Valve 220 parse error at line {token.Line}, column {token.Column}: {message}",
                innerException);
        }
    }
}
