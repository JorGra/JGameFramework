# Cursor System Setup

## Concepts

- A **cursor set** is a themed collection for a gameplay context (e.g. `MainMenu`, `Gameplay`). Switching sets swaps the whole cursor "theme" at once.
- A **cursor preset** is a single cursor definition (texture, hotspot, mode, optional visibility override) inside a set — e.g. the `Gameplay` set might contain `Default`, `UI`, `Build`, `InvalidTarget`. Each set nominates a default preset used when a request omits the preset ID or asks for something unknown.
- A **claim** is a declarative request for a cursor at a given **priority layer**. The highest-priority claim wins; disposing a claim reveals the claim beneath it. This replaces imperative "set the cursor now" calls — transient states (build mode, pause, hover) restore automatically.

### Priority layers (`CursorLayer`)

| Layer | Value | Typical source |
|---|---|---|
| `Default` | 0 | Controller's own baseline claim |
| `Scene` | 50 | `CursorScenePresetSelector`, legacy events |
| `GameState` | 100 | Pause, stage end |
| `Tool` | 200 | Build mode, active tools |
| `HoverWorld` | 300 | Hovering world objects |
| `HoverUI` | 400 | Hovering UI elements |
| `System` | 500 | Cinematics, hard overrides |

Ties at equal priority: the newest claim wins.

## Setup

1. **Author a preset library**
   - Create a `CursorPresetLibrary` asset via `Assets > Create > JGameFramework > Cursor System > Cursor Preset Library`.
   - Define a cursor set per context; add presets (pointer, hover, crosshair, build, …). Import textures with `Texture Type = Cursor` for the hardware presenter; the overlay presenter has no import requirements (Read/Write not needed).
   - Mark the fallback preset as the set's default preset.

2. **Add the runtime controller**
   - Drop `MouseCursorController` onto a bootstrap or persistent scene object (see `CursorChanger.prefab`). Assign the preset library and the default set/preset.
   - **Presenter mode**: `Auto` uses the **overlay cursor** on Linux (software cursor drawn as a top-most UI image — avoids the Linux dual-cursor and OS-scaling problems) and the hardware cursor elsewhere. Force either via the enum. The overlay cursor's size is a fraction of screen height, so it stays consistent across resolutions.

3. **Add the hover probe** (optional but recommended)
   - Put `CursorHoverProbe` on the same object as the controller. It watches what the pointer hovers: UI first (any raycast-target UI blocks world hover), then 2D world colliders via the serialized layer mask.
   - Give any world object (with a `Collider2D`) or UI element a `CursorHintOnHover` component and pick a preset — done, no code.
   - For conditional cursors implement `ICursorHoverTarget` yourself; the `CursorQueryContext` carries pointer/camera info plus an optional game-provided context object (`CursorQueryContext.GameContextProvider`).
   - Multiplayer/custom input: implement `ICursorPointerSource` and assign it to the probe.

## Requesting cursors from code

Push a claim; keep the handle; dispose it to restore whatever lies beneath:

```csharp
using JG.CursorSystem;

CursorClaimHandle claim;

void EnterBuildMode() =>
    claim = CursorClaimStack.Push("Build", "Gameplay", CursorLayer.Tool, owner: this);

void ExitBuildMode()
{
    claim?.Dispose();   // build cursor gone, previous cursor restored
    claim = null;
}
```

- `handle.Update(presetId)` changes the shown preset without re-stacking.
- Pass `visibility:`/`lockMode:` to hide the cursor or lock it (cinematics, gameplay).
- Always pass `owner: this` — claims whose Unity owner is destroyed are pruned automatically, so a forgotten dispose can't stick the cursor forever.
- Scene defaults: use `CursorScenePresetSelector` in a scene; it claims on enable and releases on scene unload.

### Legacy path

`CursorChangeRequestEvent` still works but is obsolete: each event replaces a single claim at `Scene` priority. Prefer claims — they compose and restore; events overwrite.

## Troubleshooting

- **Cursor doesn't change:** ensure an active `MouseCursorController` with a `CursorPresetLibrary` exists (bootstrap scene), and the requested set/preset IDs exist with valid textures. Enable `logWarnings`/`logInfo` on the controller for details.
- **Hover has no effect:** check the probe is enabled (only one may be active), the object has a `Collider2D` in the probe's layer mask (world) or a raycast-target Graphic (UI), and the `CursorHintOnHover` preset ID exists.
- **Wrong cursor while over UI:** intended — any raycast-target UI blocks world hover. Disable `Raycast Target` on decorative graphics.
- **Linux dual cursor / wrong size:** use the overlay presenter (`Auto` already picks it on Linux).
