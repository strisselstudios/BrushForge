# BrushForge Immutable Document Editing

## 1. Purpose

BrushForge map documents, entities, properties, and brushes remain immutable.

Editing operations return new model objects and do not mutate the original
document, entity, property, or brush collections. This allows generators,
preview state, undo history, project persistence, and export validation to
share model instances safely.

## 2. Entity property operations

MapEntity editing supports:

- Replacing the complete ordered property collection
- Appending, inserting, replacing, and removing by index
- Setting one case-insensitive property value
- Removing all properties matching a case-insensitive key
- Replacing classname through the same single-property behavior

SetSingleProperty has explicit duplicate-key behavior:

1. The first matching property position is retained.
2. That first match is replaced using the supplied key and value.
3. Later matching properties are removed.
4. When no match exists, the new property is appended.

Duplicate keys remain supported when constructed or inserted explicitly.

## 3. Entity brush operations

MapEntity editing supports:

- Replacing the complete ordered brush collection
- Appending a brush
- Inserting a brush
- Replacing a brush by index
- Removing a brush by index

Brush objects are reused by reference unless an operation explicitly replaces
one.

## 4. Document entity operations

MapDocument editing supports:

- Replacing the complete ordered entity collection
- Appending an entity
- Inserting an entity
- Replacing an entity by index
- Removing an entity by index

These general operations may temporarily produce documents that are not valid
for export. MapExportValidator remains the authority for export readiness.

## 5. Worldspawn operations

SetWorldspawn:

- Requires an entity whose classname is worldspawn
- Places the supplied worldspawn at entity index zero
- Removes every previous worldspawn
- Preserves the relative order of all non-worldspawn entities

EditWorldspawn:

- Requires an existing worldspawn
- Applies an immutable callback
- Rejects null results
- Rejects results whose classname is not worldspawn
- Normalizes the edited worldspawn through SetWorldspawn

## 6. Index behavior

Insertion indexes accept values from zero through the current collection count.

Replacement and removal indexes accept values from zero through count minus
one.

Invalid indexes throw ArgumentOutOfRangeException before a new model object is
created.

## 7. Round-trip compatibility

Documents produced by the editing API use the same model consumed by the Valve
220 reader and writer.

An edited document must continue to satisfy canonical round-trip behavior:

write(parse(write(editedDocument))) == write(editedDocument)
