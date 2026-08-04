# BrushForge Project JSON

## 1. Purpose

BrushForge project JSON is the deterministic text representation of the
immutable BrushForge project model.

It preserves editable project identity and shared workspace settings without
embedding Valve 220 map output. The JSON representation is intended for future
project-file save and open services, version migration, source control, and
diagnostic inspection.

Section 1.9 serializes and parses JSON text only. File-system paths, recent-file
state, atomic file replacement, backups, autosave, and WPF commands remain
separate responsibilities.

## 2. Canonical structure

The current schema writes properties in this order:

1. schemaVersion
2. metadata
3. settings

Metadata writes:

1. projectId
2. name
3. createdUtc
4. modifiedUtc

Settings writes:

1. activeWorkspace
2. generationSeed
3. gridSpacing
4. mapUnitsPerSourceUnit

The serializer emits UTF-8-compatible text with an LF final newline.
Indented and compact output preserve the same property order and values.

## 3. Stable value formats

Project identifiers use the lowercase-compatible Guid `D` format.

Timestamps use the round-trip `O` format and must retain a zero UTC offset.

Workspace values are lowercase stable names:

- foliage
- terrain

Generation seeds are stored as decimal strings rather than JSON numbers. This
preserves the full unsigned 64-bit range when project JSON is inspected or
processed by systems whose numeric type cannot exactly represent every UInt64
value.

Grid spacing and coordinate scale are finite positive JSON numbers.

## 4. Strict parsing

The reader rejects:

- Malformed JSON
- A non-object root
- Missing required properties
- Unknown properties
- Duplicate properties
- Incorrect JSON value types
- Unsupported schema versions
- Invalid project identifiers
- Non-UTC or malformed timestamps
- Unknown workspace names
- Invalid generation seeds
- Non-positive or non-finite numeric settings

Property names and stable string values are case-sensitive.

Strict parsing prevents misspelled or partially migrated project data from
silently entering application state.

## 5. Canonical round trips

For every valid project:

serialize(deserialize(serialize(project))) == serialize(project)

The project model constructors remain the final validation authority after JSON
values are parsed.
