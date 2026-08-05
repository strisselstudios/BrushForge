# BrushForge Tree Workflow

## 1. Purpose

Section 1.12 connects the deterministic tree generator to the first usable
BrushForge desktop workflow.

The window accepts tree dimensions, canopy complexity, generation seed, grid
spacing, and texture names. Generate Tree creates the same validated brush
geometry used by Valve 220 export.

## 2. Actual geometry preview

The preview is not a concept mockup. Each displayed box is built from the
Bounds3d associated with an actual GeneratedTreeBrush.

The trunk and every canopy layer therefore use the same positions and sizes as
the brushes written to the exported map.

The first preview intentionally uses simple role colors:

- Brown for trunk brushes
- Green for canopy brushes

Texture-image rendering and WAD material previews remain later features.

## 3. Input behavior

The input boundary parses numbers using invariant formatting. Decimal values
must use a period.

The parser then constructs TreeGenerationSettings, so all generator rules
remain authoritative, including:

- Positive finite dimensions
- One to eight canopy layers
- Canopy height below overall height
- Trunk width no greater than canopy width
- Sufficient grid units for every canopy layer
- Valid Valve 220 texture names

Invalid input clears the current preview and disables export.

## 4. Export

Export .map opens a standard Windows save dialog and writes the current
MapDocument through Valve220MapWriter.

The output is standard TrenchBroom-compatible Valve 220 map syntax. BrushForge
does not add a DUSK-specific compatibility mode.

## 5. Current vertical-slice limits

This first usable workflow generates one layered box-canopy tree family.

The following remain future work:

- Additional tree families
- Bush and rock generators
- Slider controls and presets
- WAD-backed preview materials
- Camera interaction
- Project open/save commands in the window
- Terrain generation
