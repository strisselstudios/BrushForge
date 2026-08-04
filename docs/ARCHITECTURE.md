# BrushForge Architecture

## 1. Project responsibilities

### 1.1 BrushForge.Core

Shared infrastructure with no dependency on another BrushForge project:

- Numeric tolerances
- Coordinate-system conventions
- Grid rules
- Seeded random generation
- Diagnostics
- Cancellation-aware operation contracts
- Shared identifiers and value objects

### 1.2 BrushForge.Geometry

Engine-independent brush geometry:

- Three-dimensional vectors
- Planes
- Polygon windings
- Brush faces
- Convex brushes
- Bounds
- Convexity validation
- Grid snapping
- Texture axes

This project contains no WPF, WAD parsing, file dialogs, or terrain generation.

### 1.3 BrushForge.MapFormat

Valve 220 map-format support:

- Tokenization
- Parsing
- Serialization
- Entity properties
- Brush-face syntax
- Stable numeric formatting
- Export validation

### 1.4 BrushForge.ProjectModel

Serializable BrushForge project state:

- Project metadata
- Workspace settings
- Terrain settings
- Texture assignments
- Undo and redo contracts
- Project schema versions

### 1.5 BrushForge.Generation

Deterministic generation systems:

- Flat terrain
- Heightfields
- Heightmap conversion
- Progress reporting
- Cancellation
- Brush-count budgets
- Complexity enforcement

Generators return geometry and do not write files or manipulate UI controls.

### 1.6 BrushForge.Rendering

Windows-specific preview rendering:

- Geometry-to-preview conversion
- Camera state
- Wireframe rendering
- Viewport scene construction
- Preview geometry reduction

Rendering does not modify project geometry.

### 1.7 BrushForge.WadIntegration

WAD and WadForge integration:

- WAD2 and WAD3 directory inspection
- Texture metadata
- Texture-name validation
- WadForge alias manifests
- Texture lookup catalogs

### 1.8 BrushForge.App

WPF application responsibilities:

- Windows and controls
- View models
- Commands
- Workspace navigation
- File selection
- Background-job orchestration
- Error presentation

Business and geometry rules remain in lower-level libraries.

### 1.9 BrushForge.Tests

Deterministic automated tests for non-UI systems.

## 2. Dependency rules

Allowed dependency direction:

- Core has no BrushForge dependencies.
- Geometry depends on Core.
- MapFormat depends on Core and Geometry.
- ProjectModel depends on Core and Geometry.
- Generation depends on Core, Geometry, and ProjectModel.
- Rendering depends on Core and Geometry.
- WadIntegration depends on Core.
- App may depend on every library.
- Tests may depend on platform-independent libraries.

No lower-level project may reference BrushForge.App.
