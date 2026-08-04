# BrushForge Geometry Conventions

## 1. Coordinate system

BrushForge uses a right-handed coordinate system:

- Positive X points right.
- Positive Y points forward.
- Positive Z points up.
- X cross Y equals Z.

All internal geometric calculations use double precision.

## 2. Plane representation

Planes use the normalized equation:

dot(normal, point) = distance

The order of the three source points determines face orientation.

For a valid convex brush, face normals must point outward. Full validation of
closure, convexity, finite volume, and interior half-spaces belongs to the
convex-brush validator.

## 3. Bounds

Bounds are finite axis-aligned boxes. Minimum components may equal maximum
components, allowing bounds for planar or point selections. Generated
three-dimensional brushes will later require non-zero extent on every axis.

## 4. Texture axes

BrushForge stores Valve 220 texture mapping as:

- U direction
- U offset
- V direction
- V offset
- Rotation
- U scale
- V scale

Generated axes use deterministic Valve-style axial projection. Imported axes
will be preserved rather than silently normalized or regenerated.

## 5. Brush representation

ConvexBrush is initially an immutable structural face collection. Construction
enforces:

- At least four faces
- No null faces
- No duplicate oriented planes

Construction does not yet prove that the brush is closed, bounded, convex, or
non-zero in volume. Those requirements belong to the validation system.
