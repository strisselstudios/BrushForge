# BrushForge Tree Generation Foundation

## 1. Purpose

Section 1.11 introduces the first production foliage generator used by the
BrushForge vertical slice.

The generator creates actual convex brush geometry rather than a preview-only
placeholder. Its output can be rendered by the future preview layer and written
directly through the existing Valve 220 map writer.

## 2. Generated structure

One tree contains:

- One axis-aligned trunk brush
- One through eight stacked canopy brushes
- Semantic role metadata for trunk and canopy parts
- One worldspawn entity containing every generated brush
- Generator and seed metadata stored on worldspawn

The initial tree family intentionally uses a low brush count. More complex tree
families can reuse the same result and role model without changing the export
pipeline.

## 3. Determinism

Tree variation is produced through BrushForge's owned deterministic random
number generator.

The same settings and generation seed always produce the same brush order,
face data, canopy offsets, and Valve 220 document.

Changing the seed may alter canopy layer offsets and depth while preserving the
requested dimensions and brush-count budget.

## 4. Grid policy

The generator snaps the tree origin and dimensions to the selected project
grid.

Every generated brush vertex lies on that grid. This keeps exported geometry
predictable and straightforward to edit in TrenchBroom.

The canopy receives at least one grid unit of height per layer. The trunk
extends one grid unit into the canopy so the generated structure does not have
a visible separation.

## 5. Bounds and semantic parts

Each generated part contains:

- TreeBrushRole
- Canopy layer index
- ConvexBrush
- Axis-aligned Bounds3d

The complete result also exposes the ordered brush collection, combined tree
bounds, and Valve 220 MapDocument.

These values will feed the real-time preview, material mapping, statistics, and
export controls in later sections.

## 6. Current limitations

The first generator uses rectangular trunk and canopy brushes only.

It does not yet provide:

- Branch brushes
- Angled trunks
- Multiple tree species
- Bushes or rocks
- Material profile selection
- WPF controls or preview rendering

Those systems will build on this validated generation and export path rather
than replacing it.
