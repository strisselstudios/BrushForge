# BrushForge Valve 220 Map Reading

## 1. Supported document syntax

The reader accepts ordered Valve 220 entities containing:

- Quoted entity keys and values
- Convex brushes
- Three-point face planes
- Unquoted texture names
- Explicit U and V texture axes
- Rotation and independent U/V scale
- `//` line comments

Source comments and whitespace are discarded. Writing a parsed document produces
BrushForge's canonical formatting.

## 2. Token positions

Every token records a one-based line and column.

Lexical and grammatical failures produce an InvalidDataException containing
the source position and a specific expected-token description.

## 3. Strings

Quoted strings decode:

- `\\` as one backslash
- `\"` as one quotation mark

Unknown backslash escapes are preserved rather than silently deleting the
backslash.

Quoted strings cannot span lines.

## 4. Numbers

Numbers are parsed using invariant culture and may use decimal or scientific
notation.

NaN and infinity are rejected.

## 5. Transparent textures

Texture names may begin with `{`, which is conventionally used by transparent
WAD textures.

Opening braces anywhere except the first character remain invalid. Closing
braces remain invalid because texture names are unquoted in brush-face syntax.

## 6. Structural validation

The reader constructs the same immutable model used by generated maps.

A parsed brush must contain at least four distinct oriented face planes.
Complete closure, volume, and convexity validation remains the responsibility
of ConvexBrushValidator and MapExportValidator.

## 7. Encoding

ParseFile reads strict UTF-8 and accepts files with or without a UTF-8
byte-order mark.

The writer continues to emit UTF-8 without a byte-order mark.

## 8. Round trips

Canonical output must satisfy:

write(parse(write(document))) == write(document)

This guarantees that BrushForge-generated Valve 220 documents can be read back
without changing entity order, property order, geometry, texture axes, or
canonical formatting.
