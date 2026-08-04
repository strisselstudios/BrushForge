# BrushForge Valve 220 Map Writing

## 1. Document structure

A map document contains ordered entities.

Each entity contains:

1. Ordered quoted key/value properties
2. Ordered convex brushes

Repeated property keys are preserved. This is necessary for lossless handling
of map dialects that use repeated keys.

## 2. Worldspawn requirements

A valid export requires exactly one worldspawn entity.

The worldspawn entity must be first.

Every entity requires a classname property.

## 3. Brush validation

Every brush is passed through ConvexBrushValidator before normal export.

A document containing an open, unbounded, degenerate, reversed, or otherwise
invalid brush is rejected.

Validation may be disabled only for controlled diagnostic output. User-facing
export must keep validation enabled.

## 4. Face syntax

Brush faces are written using Valve 220 syntax:

( p1 ) ( p2 ) ( p3 ) texture [ ux uy uz uOffset ] [ vx vy vz vOffset ] rotation uScale vScale

The three points retain their original orientation and define the infinite
brush plane.

## 5. Numeric formatting

All numbers use invariant culture.

Canonical output:

- Uses a period as the decimal separator
- Omits unnecessary trailing zeroes
- Emits zero as `0`
- Rejects NaN and infinity
- Does not depend on Windows regional settings

## 6. String formatting

Entity keys and values are quoted.

Backslashes and quotation marks are escaped. Null characters and line breaks
are rejected.

Texture names are not quoted because Valve brush-face syntax requires an
unquoted texture token. BrushFace validates those names before serialization.

## 7. Encoding

Map files are written as UTF-8 without a byte-order mark.

The default line ending is CRLF. LF output remains available for deterministic
cross-platform testing.
