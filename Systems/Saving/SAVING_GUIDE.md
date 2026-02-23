# JGameFramework Saving Layer

Synchronous save API. Non-cached cases write to disk immediately. Cached cases batch writes in memory and flush to disk on app pause, quit, or manual `Flush()`.

## Quick Start

```csharp
// Bootstrap — pick a backend, optionally pass a SaveConfig asset
SaveSystem.Init(new Es3Backend(), saveConfig);

// Or without config (uses "slot_default", no caching)
SaveSystem.Init(new Es3Backend());

// Save / Load
SaveSystem.Save(SaveCases.Meta, myData);
var data = SaveSystem.Load(SaveCases.Meta, defaultValue: new MyData());
```

## Defining Cases

All cases live in `Assets/4. Scripts/Saving/SaveCases.cs`. To add a new case, add a field using `nameof()`:

```csharp
public static class SaveCases
{
    public static readonly SaveCaseId Meta = new(nameof(Meta));
    public static readonly SaveCaseId Profile = new(nameof(Profile));
    public static readonly SaveCaseId Run = new(nameof(Run));
}
```

The field name becomes the file key (e.g. `Meta` → `Meta.es3`). `nameof()` guarantees they match — no raw strings. New cases automatically appear in the SaveConfig inspector dropdown.

## Caching

By default, `Save` writes to disk immediately and `Load` reads from disk. For cases with frequent writes, enable caching in the **SaveConfig** asset (toggle `Cached`), or from code:
```csharp
SaveSystem.SetCached(SaveCases.Run);
```

**Cached behavior:**
- `Save` writes to memory only. Disk write is deferred.
- `Load` returns from memory if available, otherwise reads from disk.
- `Flush()` writes all pending cached saves to disk.
- Auto-flush fires on app pause and quit via a `DontDestroyOnLoad` hook.
- `UseSlot` flushes before switching.

**Non-cached behavior (default):**
- `Save` writes to disk immediately.
- `Load` reads from disk.

## SaveConfig

Optional ScriptableObject (**Right-click > JGameFramework > Saving > SaveConfig**). Controls:
- **Default Slot ID** — which slot to use at startup.
- **Cases** — list of `SaveCaseId` + `Cached` flag.

## Slots

- Active slot defaults to `"slot_default"`.
- Switch: `SaveSystem.UseSlot("player_2");` (flushes pending writes, clears cache).
- Delete: `SaveSystem.DeleteSlot("player_2");` (discards pending writes for that slot).

## Backends

- **MemoryBackend** — in-memory, good for testing.
- **Es3Backend** — Easy Save 3 adapter (requires `JG_SAVING_ES3` define).
- Custom: implement `ISaveBackend`.

## File Layout (Es3Backend)

```
<persistentDataPath>/slots/<slotId>/<caseId>.es3
```

## Safety

- All backend calls are wrapped in try/catch. A corrupted file won't crash the game.
- If `Init` is never called, a fallback in-memory backend is created with a warning.
- Cached writes auto-flush on pause/quit. On force-kill (iOS/Android), pending writes are lost — same trade-off as any write buffer.

## Full API

```csharp
SaveSystem.Init(backend, config);     // config is optional
SaveSystem.SetCached(caseIds);        // code-driven caching (additive with config)
SaveSystem.Save<T>(caseId, value);
SaveSystem.Load<T>(caseId, defaultValue);
SaveSystem.Exists(caseId);
SaveSystem.Delete(caseId);
SaveSystem.Flush();                   // manual flush (auto on pause/quit)
SaveSystem.UseSlot(slotId);
SaveSystem.DeleteSlot(slotId);
```
