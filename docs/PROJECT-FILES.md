# BrushForge Project Files

## 1. Purpose

BrushForge project files use the `.brushforge` extension and contain the
canonical JSON representation defined by the project-model serialization
layer.

The storage layer is independent from WPF. File dialogs, recent-project lists,
autosave policy, and user prompts remain application-shell responsibilities.

## 2. File format metadata

`BrushForgeProjectFileFormat` exposes the supported extension and provides
case-insensitive extension checks.

`EnsureExtension` preserves an existing `.brushforge` suffix and otherwise
adds or replaces the current extension.

## 3. Loading

`BrushForgeProjectFileStore.Load`:

- Resolves the supplied path to an absolute path
- Reads the complete file using strict UTF-8 decoding
- Accepts an optional UTF-8 byte-order mark
- Rejects malformed UTF-8 as `InvalidDataException`
- Delegates project validation to the strict JSON serializer
- Preserves normal file-system exceptions such as `FileNotFoundException`

## 4. Saving

`BrushForgeProjectFileStore.Save`:

1. Serializes the immutable project before changing the destination.
2. Creates missing parent directories.
3. Writes canonical JSON to a uniquely named temporary file in the destination
   directory.
4. Moves that completed file over the destination using overwrite semantics.
5. Deletes any remaining temporary file when writing or replacement fails.

Writing the temporary file in the destination directory keeps the final move on
the same file system and prevents readers from observing a partially written
project file.

Saved files use UTF-8 without a byte-order mark and retain the serializer's
canonical LF line endings.

## 5. Scope boundary

This layer does not yet provide:

- Open or Save As dialogs
- Current-document dirty state
- Recent-project history
- Autosave or recovery files
- Project locking
- WPF commands or view models

Those systems can build on this storage API without changing the project file
contract.
