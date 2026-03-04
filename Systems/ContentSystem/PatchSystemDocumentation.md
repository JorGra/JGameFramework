# Content Patch System: Developer Guide

This document covers the architecture, APIs, and extension points of the
content patch system. It is aimed at programmers working on the game code,
the content pipeline, or the ModCreator editor.

---

## Architecture Overview

The patch system allows mods to declare surgical modifications to existing
content entries without replacing them entirely. Patches operate at the
JSON token level (using Newtonsoft.Json's `JToken`), which means they work
on any content type without needing type-specific code.

```
Mods/Core/Items/Towers/towers.json            <-- original entries
Mods/BalanceMod/Items/Towers/rebalance_patches.json  <-- patches
```

The pipeline has three layers:

| Layer             | Technology   | Purpose                                      |
|-------------------|-------------|----------------------------------------------|
| Data model        | C# classes   | `ContentPatch` and `PatchOperation` POCOs    |
| Patch engine      | C# static   | `ContentPatchApplier` applies ops to JTokens |
| Import pipeline   | C# MonoBehaviour | `JsonContentImporter` orchestrates two-pass loading |
| ModCreator editor | Svelte/JS    | Visual patch editing UI                      |

---

## Data Model

### File

`Assets/JGameFramework/Systems/ContentSystem/Scripts/Core/ContentPatch.cs`

### Classes

```csharp
public sealed class ContentPatch
{
    public string TargetId;        // The entry ID being patched ($patch value)
    public string SourceMod;       // Mod that declared this patch (for error reporting)
    public string SourceFile;      // File path of the patch file
    public List<PatchOperation> Ops;
}

public sealed class PatchOperation
{
    public string Op;              // "set", "remove", "add", "merge"
    public string Path;            // Slash-separated path with [key=value] match syntax
    public JToken Value;           // Value for set/merge/add (single item)
    public JArray Values;          // Values for add (multiple items)
    public int? Index;             // Optional insert position for add
}
```

These are plain data containers. They are constructed during patch file
parsing in `JsonContentImporter` and consumed by `ContentPatchApplier`.

---

## Patch Engine

### File

`Assets/JGameFramework/Systems/ContentSystem/Scripts/Core/ContentPatchApplier.cs`

### Public API

```csharp
public static class ContentPatchApplier
{
    // Apply all operations in a patch to a JToken. Returns a new (cloned) token.
    public static JToken Apply(JToken original, ContentPatch patch);

    // Navigate a slash-separated path to resolve a JToken.
    public static JToken ResolvePath(JToken root, string path);
}
```

### Path Resolution

`ResolvePath` splits the path on `/` and resolves each segment:

| Segment Format    | Behavior                                                |
|-------------------|---------------------------------------------------------|
| `PropertyName`    | Object property lookup (case-insensitive)               |
| `0`, `1`, `2`     | Array index                                             |
| `[key=value]`     | First array item where property `key` equals `value` (case-insensitive) |

Examples:

```
"Price"                              --> root.Price
"Modules/0"                          --> root.Modules[0]
"Modules/[type=Turret]"              --> first Modules item with type == "Turret"
"Modules/[type=Turret]/speed"        --> speed property on that matched item
"BaseStats/[statId=core.stat.range]" --> first BaseStats item matching statId
```

Path resolution is case-insensitive at every level. This matches the
existing case-insensitive ID handling in `ContentCatalogue`.

### Operations

#### Set

Resolves all path segments except the last one to find the parent, then
assigns the value at the final key.

```csharp
private static void ApplySet(JToken root, PatchOperation op)
```

- On a `JObject`: sets the property (creates it if missing).
- On a `JArray`: replaces the item at the given index.
- If the last segment is a `[key=value]` match, it resolves to an index
  first.

#### Remove

Resolves the parent and removes the final key.

```csharp
private static void ApplyRemove(JToken root, PatchOperation op)
```

- On a `JObject`: removes the property.
- On a `JArray`: removes the item at the resolved index.

#### Add

Resolves the full path to find the target array, then inserts items.

```csharp
private static void ApplyAdd(JToken root, PatchOperation op)
```

- Uses `op.Value` for a single item or `op.Values` for multiple items.
- Uses `op.Index` for positional insert; defaults to appending.
- The index is clamped to `[0, array.Count]`.

#### Merge

Resolves the full path to find the target object, then deep-merges.

```csharp
private static void ApplyMerge(JToken root, PatchOperation op)
```

- Recursively merges nested `JObject` properties.
- Non-object properties in the merge source overwrite the target.
- The merge value must be a `JObject`.

### Error Handling

Each operation is wrapped in a try-catch. On failure, a `Debug.LogWarning`
is emitted with the operation type, path, target ID, source file, and
error message. The remaining operations in the same patch continue to
execute.

If `ResolvePath` cannot resolve a segment, it returns `null`. Operations
that receive a `null` parent or target log a warning and skip.

---

## ContentCatalogue Extensions

### File

`Assets/JGameFramework/Systems/ContentSystem/Scripts/Core/ContentCatalogue.cs`

### New Fields

```csharp
// Raw JToken storage for patch support -- keyed by ID (case-insensitive)
private readonly ConcurrentDictionary<string, JToken> _rawTokens;

// Tracks which def Type was used to register each ID
private readonly ConcurrentDictionary<string, Type> _defTypeById;
```

### New Methods

```csharp
// Store a raw JToken and its def type after deserialization
public void StoreRawToken(string id, JToken token, Type defType);

// Retrieve the raw JToken for patching
public JToken GetRawToken(string id);

// Retrieve the original def type for re-deserialization after patching
public Type GetDefType(string id);
```

### Clear

`Clear()` now resets `_rawTokens` and `_defTypeById` alongside `_tables`.

### Why Raw Tokens?

Patches operate at the JSON level, before C# deserialization. Storing the
raw `JToken` at registration time lets the patch engine work on the
original JSON structure without needing to serialize back from the C#
object (which would lose unknown fields and formatting).

---

## Import Pipeline

### File

`Assets/JGameFramework/Systems/ContentSystem/Scripts/Core/JsonContentImporter.cs`

### Two-Pass Import

The `Import()` method now processes each content folder in two passes:

```
For each defType:
  Pass 1: Import all *.json files EXCEPT *_patches.json
  Pass 2: Import all *_patches.json files
```

This ensures that all base entries exist in the catalogue before patches
are applied. Within a single mod, patches always see the complete set of
that mod's own entries plus everything from previously loaded mods.

### Patch File Detection

```csharp
public static bool IsPatchFile(string path)
{
    var name = Path.GetFileNameWithoutExtension(path);
    return name.EndsWith("_patches", StringComparison.OrdinalIgnoreCase);
}
```

Any file whose name (without `.json`) ends with `_patches` is treated as
a patch file and excluded from regular content loading.

### Raw Token Storage

In `DeserializeAndRegister()`, after successfully deserializing and
registering a content def, the raw JToken is stored:

```csharp
ContentCatalogue.Instance.StoreRawToken(def.Id, token, defType);
```

This is called for both regular imports and re-registrations after
patching, so the catalogue always holds the latest raw state.

### ApplyPatchFile

```csharp
private static void ApplyPatchFile(string filePath, IModHandle h, string modId)
```

This method:

1. Reads the patch file as a `JArray` of patch entries.
2. For each entry, extracts `$patch` (target ID) and `ops` (operations).
3. Constructs a `ContentPatch` with parsed `PatchOperation` objects.
4. Retrieves the target's raw `JToken` and `Type` from `ContentCatalogue`.
5. Calls `ContentPatchApplier.Apply()` to produce the patched token.
6. Calls `DeserializeAndRegister()` with the patched token to update the
   catalogue (which also updates the stored raw token for future patches).
7. Logs success or warnings.

If the target ID is not found in the catalogue, the entire patch entry is
skipped with a warning.

---

## Mod Load Order and Layering

Patches follow the existing mod load order defined by the manifest
(`requires`, `loadAfter`, `loadBefore`). Within each mod:

```
Mod A loads:
  1. Regular entries registered to catalogue (raw tokens stored)
  2. Patch files applied (patched tokens re-registered)

Mod B loads:
  1. Regular entries (may override Mod A entries)
  2. Patch files (operate on the current catalogue state, including
     Mod A's patches and Mod B's own entries)
```

This means:

- **Patches compose.** Mod A can patch a Core entry, and Mod B can patch
  the same entry. Both sets of changes apply in order.
- **Patches see prior patches.** Mod B's patches operate on the
  already-patched state from Mod A.
- **Regular overrides take priority.** If Mod B provides a full entry with
  the same ID, it replaces everything (including Mod A's patches). Then
  Mod B's own patches apply on top.

---

## ModCreator Editor

### Service: patchCrudService.js

`ModCreator/ModCreator/svelte/src/services/patchCrudService.js`

Pure functions for patch file I/O. No UI state.

| Function | Purpose |
|----------|---------|
| `isPatchFile(name)` | Check if a filename ends with `_patches.json` |
| `loadPatchFile(modHandle, filePath)` | Read a single patch file |
| `readPatchFiles(modHandle, contentFolder)` | Read all patch files in a folder |
| `savePatchEntry({ modHandle, contentFolder, targetId, ops, fileName })` | Create or update a patch entry |
| `deletePatchEntry({ modHandle, filePath, targetId })` | Remove a patch entry from a file |
| `buildArrayItemPath(item, itemSchema, index)` | Determine best `[key=value]` path for an array item |
| `validatePatchOp(op)` | Validate a patch operation object |

**Save behavior:** `savePatchEntry` reads the existing patch file (if any),
finds an existing entry with the same `$patch` target ID, and replaces it.
If no existing entry is found, the new patch is appended. If no file
exists, it is created.

**Delete behavior:** `deletePatchEntry` removes the matching entry from the
file. If the file becomes empty, it is deleted entirely.

### Store: patchEditorStore.js

`ModCreator/ModCreator/svelte/src/stores/patchEditorStore.js`

Manages in-memory editing state for patches. Follows the same context-map
pattern as `entryEditorStore.js`.

**Reactive stores:**

| Store | Type | Description |
|-------|------|-------------|
| `currentPatchContext` | `object\|null` | Full context for the active patch |
| `currentPatchOps` | `Array` | Operations list for the active patch |
| `currentPatchTargetId` | `string\|null` | Target entry ID |
| `currentPatchDirty` | `boolean` | Unsaved changes? |
| `currentPatchKey` | `string\|null` | Active context key |
| `allPatchContexts` | `Map` | All open patch contexts |
| `patchVersion` | `number` | Incremented on save (triggers list reload) |

**Actions:**

| Action | Description |
|--------|-------------|
| `startPatch({ targetId, contentFolder, ops, fileName })` | Open a patch for editing |
| `selectPatch(targetId, contentFolder)` | Switch to an existing context |
| `updatePatchOps(ops)` | Replace all operations |
| `addPatchOp(op)` | Append an operation |
| `removePatchOp(index)` | Remove an operation by index |
| `updatePatchOpAt(index, op)` | Replace a specific operation |
| `movePatchOp(index, direction)` | Reorder an operation (-1 = up, +1 = down) |
| `markPatchSaved()` | Clear dirty flag, increment version |
| `removePatchContext(targetId, contentFolder)` | Remove a context |
| `clearPatchContexts()` | Clear all contexts |

### Schema Utility: getArrayItemMatchKey

`ModCreator/ModCreator/svelte/src/services/schemaUtils.js`

```javascript
export function getArrayItemMatchKey(arraySchema, rootSchema)
```

Returns the best field name for `[key=value]` path syntax in arrays.
Checks for discriminator fields (`TypeId`, `type`, `$type`) first, then
common ID fields (`statId`, `id`, `key`, `name`), and returns `null` as
fallback (use index).

### modDataService.js Changes

`readDefinitionFiles()` now skips files where `isPatchFile(name)` returns
true. This prevents patch files from being parsed as regular content
entries in the ModCreator.

### UI Component: PatchPanel.svelte

`ModCreator/ModCreator/svelte/src/components/workspace/PatchPanel.svelte`

Top-level panel accessible from the "Patches" tab in the app header. Layout:

```
+------------------+---------------------------------------+
| Patch Sidebar    | Patch Editor                          |
|                  |                                       |
| [+ New Patch]    | Patch: core.tower.ballisticTurret     |
|                  |        [Unsaved] [Save Patch]         |
| > core.tower...  |                                       |
|   Items/Towers   | Operations (3)                        |
|   3 ops          | +-----------------------------+       |
|                  | | SET  Price                  |       |
| > core.enemy...  | | 20                          |       |
|   Enemies        | +-----------------------------+       |
|   1 op           | | REMOVE BaseStats/[statId=..]|       |
|                  | +-----------------------------+       |
|                  | | ADD  BaseStats              |       |
|                  | | { "statId": "...", ... }    |       |
|                  | +-----------------------------+       |
|                  |                                       |
|                  | Add Operation                         |
|                  | Type: [Set v]  Path: [_________]      |
|                  | Value: [_______________]              |
|                  | [Add Operation]                       |
+------------------+---------------------------------------+
```

The panel reads all `*_patches.json` files across all content folders on
mount and when the patch version changes (after saves).

### Shell Integration

`ModCreatorShell.svelte` imports `PatchPanel` and routes to it when
`activeTab === "patches"`. `AppHeader.svelte` includes the "Patches" tab
button, disabled when no mods folder is connected.

---

## How-To: Adding a New Patch Operation Type

To add a new operation (e.g., `"replace"` for full array replacement):

### 1. Update the C# engine

In `ContentPatchApplier.cs`, add a case to the switch in `Apply()`:

```csharp
case "replace":
    ApplyReplace(result, op);
    break;
```

Implement the `ApplyReplace` method following the pattern of existing
operations.

### 2. Update the ModCreator

In `patchCrudService.js`, add the new op to the `validOps` array in
`validatePatchOp()`:

```javascript
const validOps = ["set", "remove", "add", "merge", "replace"];
```

In `PatchPanel.svelte`, add the option to the type dropdown and handle
any additional fields (e.g., if the new op needs special parameters).

### 3. Update the data model (optional)

If the new operation needs fields beyond `Path`, `Value`, `Values`, and
`Index`, add them to `PatchOperation.cs` and update the parsing in
`ApplyPatchFile()`.

---

## How-To: Testing Patches

### Manual Testing

1. Create a test mod with a `manifest.json` that requires the mod you want
   to patch.
2. Add a `*_patches.json` file in the appropriate content folder.
3. Load the game and check the Unity console for:
   - `Patched "targetId" (N ops)` -- success
   - `[Patch] set: could not resolve path "..."` -- path error
   - `Patch target "..." not found in catalogue` -- target not loaded yet
4. Verify the patched values in-game or via `ContentCatalogue.Instance.TryGet`.

### Programmatic Verification

```csharp
// After mod loading completes:
if (ContentCatalogue.Instance.TryGet<ItemDef>("core.tower.ballisticTurret", out var tower))
{
    Debug.Log($"Tower price: {tower.Price}");  // Should reflect patched value
}

// Inspect raw token:
var raw = ContentCatalogue.Instance.GetRawToken("core.tower.ballisticTurret");
Debug.Log(raw?.ToString(Formatting.Indented));
```

### ModCreator Testing

1. Open the ModCreator and load your mod.
2. Go to the **Patches** tab.
3. Verify your patches appear in the sidebar with correct target IDs.
4. Click a patch to see its operations.
5. Modify and save, then verify the JSON file on disk.

---

## Edge Cases and Limitations

### Target Not Found

If a patch references an entry ID that does not exist in the catalogue at
the time the patch is applied, the entire patch entry is skipped with a
warning. This can happen if:

- The target mod is not loaded (missing `requires` in manifest).
- The target entry was removed by an earlier mod.
- The target ID has a typo.

### Path Resolution Failure

If any segment of a path cannot be resolved, that individual operation is
skipped. Other operations in the same patch continue to execute.

### Multiple Patches for Same Target

Within a single patch file, if two entries share the same `$patch` target
ID, both are applied in order (first entry first). In the ModCreator,
`savePatchEntry` replaces the existing entry for the same target.

Across different patch files in the same mod, all patches are applied.
Across different mods, patches are applied in mod load order.

### Patching Patched Entries

When a patch is applied, the result is re-registered in the catalogue
(including updating the stored raw token). Subsequent patches -- whether
from the same mod or later mods -- operate on the already-patched state.

### Regular Override vs. Patch

If a mod provides both a regular entry and a patch for the same ID, the
regular entry is registered first (Pass 1), then the patch is applied
(Pass 2). This is valid but unusual -- typically a mod either provides
full entries or patches, not both for the same ID.

### Performance

Patches add minimal overhead. Each patch file is read once, and each
operation involves a JSON tree traversal proportional to the path depth.
The re-deserialization step (after patching) is the same cost as the
initial deserialization.
