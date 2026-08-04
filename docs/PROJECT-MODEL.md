# BrushForge Project Model

## 1. Purpose

BrushForge project state is stored independently from Valve 220 map output.

The project model represents the editable source state used by the application,
generators, preview system, undo history, and future project-file serializer.
Valve 220 remains an export format rather than the authoritative BrushForge
workspace format.

## 2. Project root

BrushForgeProject is the immutable root object for shared project state.

It contains:

- A supported project schema version
- Project identity and timestamps
- The active BrushForge workspace
- The deterministic generation seed
- Grid spacing
- Source-to-map coordinate scale

Section 1.8 does not yet serialize this model. Project-file JSON serialization
and migration belong to a later section.

## 3. Schema version

BrushForgeProjectSchema defines the current and minimum supported schema
versions.

Version 1 is the initial schema. Constructing a project with an unsupported
version throws InvalidDataException so unsupported project data cannot silently
enter the application state.

## 4. Metadata

BrushForgeProjectMetadata stores:

- A non-empty Guid project identifier
- A trimmed user-facing project name
- Created and modified UTC timestamps

Names cannot be blank, exceed 128 characters, or contain line breaks.

Modified time cannot precede created time. All stored timestamps must use a zero
UTC offset.

## 5. Workspaces

BrushForgeWorkspaceKind currently defines:

- Foliage
- Terrain

The foliage workspace is the default because the foliage generator is the first
generator scheduled for implementation. Rocks remain part of the foliage
generator rather than becoming a separate top-level application workspace.

## 6. Shared settings

BrushForgeProjectSettings stores settings shared across generation workspaces:

- Active workspace
- GenerationSeed
- GridSpacing
- CoordinateScale

New projects use:

- Foliage as the active workspace
- Eight map units as the grid spacing
- Identity coordinate scale
- The caller-supplied deterministic generation seed

All editing methods return a new settings object.

## 7. Immutable editing

BrushForgeProject provides immutable operations for:

- Renaming a project
- Replacing shared settings
- Switching the active workspace

Each project-level edit updates the modified timestamp while preserving project
identity and created time.

## 8. Responsibility boundary

This section intentionally excludes:

- JSON serialization
- File paths and recent-file state
- WPF view models
- Generator-specific foliage or terrain parameters
- Undo and redo storage
- Valve 220 map documents

Those responsibilities will be introduced separately so the project root
remains small and stable.
