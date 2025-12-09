# Cursor System Setup

1. **Understand sets vs presets**
   - A **cursor set** represents a themed collection for a specific gameplay context (e.g. `MainMenu`, `Gameplay`). Switching sets lets you swap the entire cursor “theme” at once.
   - A **cursor preset** is a single cursor definition (texture, hotspot, mode, optional visibility override) that lives inside a set. For example, the `Gameplay` set might contain `DefaultCrosshair`, `Interact`, and `InvalidTarget`.
   - Each set can nominate a default preset that is used whenever a request omits the preset ID or asks for something unknown.

2. **Author a preset library**
   - Create a `CursorPresetLibrary` asset via `Assets > Create > JGameFramework > Cursor System > Cursor Preset Library`.
   - Define a cursor set per context (e.g. `MainMenu`, `Gameplay`). Each set can contain multiple presets (pointer, hover, crosshair, etc.). Make sure the textures are imported with `Texture Type = Cursor`.
   - Mark the preset you want to fall back to (e.g. the gameplay crosshair) as the set's default preset.

3. **Add the runtime controller**
   - Drop `MouseCursorController` onto a bootstrap or persistent scene object. Assign the preset library and configure the default set/preset it should apply on startup.
   - The controller automatically listens for `CursorChangeRequestEvent`s on the global EventBus. No singletons are created, so you can keep the object inside whichever scene makes sense.

4. **Select presets per scene or system**
   - Use the provided `CursorScenePresetSelector` component in any scene that should swap to a given preset as soon as it loads (for example, the gameplay scene can select the crosshair preset).
   - Alternatively, raise events from gameplay/UI code directly:

     ```csharp
     EventBus<CursorChangeRequestEvent>.Raise(
         new CursorChangeRequestEvent(
             presetId: "Crosshair",
             cursorSetId: "Gameplay",
             allowFallbackToDefaultPreset: false));
     ```

5. **Optional overrides**
   - The event supports overriding cursor visibility or lock state on a per-request basis, making it easy to hide the cursor during cinematics or lock it during gameplay.

6. **Troubleshooting when the cursor doesn't switch**
   - Make sure the scene actually contains an active `MouseCursorController` with a `CursorPresetLibrary` assigned before any `CursorChangeRequestEvent` is raised. If the event fires during scene load, put the controller in a bootstrap scene or raise the event later (e.g. `Start` instead of `Awake`).
   - Confirm that the requested cursor set/preset IDs exist inside the assigned library and that the chosen preset has a valid texture (the controller logs a warning if it’s missing).
   - If nothing happens and no warnings show up, enable `logWarnings` on the controller to get detailed messages about fallback behaviour.
