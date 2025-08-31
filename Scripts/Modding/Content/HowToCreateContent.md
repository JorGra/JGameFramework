# Unity Modding & Content Pipeline — Complete Setup Guide

This guide explains, end-to-end, how your modding/content pipeline works and the exact steps to add **any** new game-related content. It is written for both:

* **Mod authors** (drop JSON + assets into a mod folder), and
* **Developers** (add a brand-new content type and make it spawnable at runtime).

Everything below is based on your provided system: a **mod loader**, a **content catalogue** filled from JSON, **ScriptableObject definitions** (`ContentDef`), and **runtime factories** that turn defs into spawnable `GameObject`s.

---

## 0) Big-picture overview

**Data flow** (one line):

```
/Mods/<YourMod> (JSON + assets)
   └─> ContentLoader  ──> ContentCatalogue (IContentDef per type)
           └─> RuntimeFactoryRegistry (per content type)
                 └─> RuntimeFactory<TDef>.Build() ──> GameObject in scene
```

* **Mod loader**: reads all mods from a known location (e.g. `Mods/`) using an **in-game Mod Manager** to define **load order**.
* **JSON**: each content type is loaded from a specific folder within a mod (via `[ContentFolder(...)]`).
* **IDs**: every piece of mod data implements `IContentDef.Id` (globally unique, **case-insensitive**).
* **Assets**: referenced by **keys** in JSON and auto-loaded into ScriptableObjects via `[AssetFromFile(...)]`.
* **Runtime**: a per-type `IRuntimeFactory<TDef>` validates and “bakes” a prefab, then `Build()` returns an instance. `RuntimeObjects.Spawn<TDef>(...)`/`TrySpawn<TDef>(...)` use these factories.

---

## 1) Folder conventions & load order

### 1.1 Mod root and content folders

Each mod lives in a folder under your mods root (e.g. `Mods/MyFirstMod/`). Within it, you must follow the **content folder** structure dictated by your definitions:

* `[ContentFolder("Items")]` → JSON anywhere under `Mods/<ModName>/Items/`
* `[ContentFolder("Items/Towers")]` → JSON anywhere under `Mods/<ModName>/Items/Towers/`

> The loader picks up **any `.json` file(s)** in that folder subtree for that content type.

**Example tree for a tower mod:**

```
Mods/
└─ MyFirstMod/
   ├─ Items/
   │  ├─ item.json                 # (optional) generic items
   │  ├─ Icons/
   │  │  └─ Turret01.png
   │  └─ Towers/
   │     ├─ towers.json            # tower defs live here
   │     └─ Sprites/
   │        ├─ Tower_BallisticTurret_Base.png
   │        └─ Tower_BallisticTurret_Head.png
   └─ (any other folders your mod uses)
```

### 1.2 Load order

Your **in-game Mod Manager** decides the **load order**. The system reads mod files in that order. Because `IContentDef.Id` is globally unique and case-insensitive, **do not** reuse IDs across mods unless you intend to replace/override (actual override behavior depends on your catalogue/loader—best practice is **unique IDs across all mods**).

---

## 2) The core contracts (what the system expects)

### 2.1 `IContentDef` & `ContentDef` (authoritative data contract)

```csharp
public interface IContentDef
{
    string Id { get; }          // globally-unique, case-insensitive
    string SourceFile { get; set; } // set by loader; path to the JSON file
}

public abstract class ContentDef : ScriptableObject, IContentDef
{
    [SerializeField] private string id;
    private string sourceFile;

    public string Id { get => id; set => id = value; }
    public string SourceFile { get => sourceFile; set => sourceFile = value; }
}
```

* **Id** must be unique. Treat it like a GUID-style key for your content.
* **SourceFile** is filled by the loader for diagnostics; don’t author it in JSON.

### 2.2 Folders per content type

Decorate each concrete `ContentDef` with `[ContentFolder("...")]`. This is the **mod-relative** folder where JSON for that type lives.

### 2.3 Asset binding with `[AssetFromFile(...)]`

Fields marked with `[AssetFromFile]` resolve keys from JSON into real assets. Two important patterns you already use:

* **Mod file lookup**: e.g. `[AssetFromFile("Items/Icons", ".png", fileNameKey: nameof(IconKey))]`

  * Looks for `Mods/<ModName>/Items/Icons/<IconKey>.png`
* **Resources lookup**: e.g. `[AssetFromFile("Resources:Prefabs/Towers", fileNameKey: nameof(TowerTemplatePrefabKey))]`

  * Looks under `Assets/Resources/Prefabs/Towers/<TowerTemplatePrefabKey>.prefab` (shipped with the base game)

> JSON holds the **key** (e.g. `"IconKey": "Turret01"`). The attribute determines **where/how** the actual asset is fetched.

---

## 3) Example content types already in your project

### 3.1 `ItemDef` (generic items)

```csharp
[ContentFolder("Items")]
[CreateAssetMenu(menuName = "Defs/Item")]
public class ItemDef : ContentDef, IInventoryItem
{
    public string DisplayName;
    public string Description;

    public string IconKey;
    [AssetFromFile("Items/Icons", ".png", fileNameKey: nameof(IconKey))]
    public Sprite Icon;

    public int Price;

    public int MaxStack { get; set; } = 99;
    public IReadOnlyList<string> EquipTags { get; set; }
    public IReadOnlyList<ItemEffectDefinition> Effects { get; set; }

    // IInventoryItem mappings...
}
```

* JSON belongs to: `Mods/<ModName>/Items/*.json`
* Icons: `Mods/<ModName>/Items/Icons/<IconKey>.png`

### 3.2 `TowerDef` (a concrete item: your tower)

```csharp
[ContentFolder("Items/Towers")]
[CreateAssetMenu(menuName = "Defs/Item - Tower")]
public class TowerDef : ItemDef
{
    public float AttackRadius;
    public float Cooldown;

    public string ProjectilePrefabKey;
    public GameObject ProjectilePrefab; // (resolved by your systems; ensure it’s set)

    public string TowerTemplatePrefabKey;
    [AssetFromFile("Resources:Prefabs/Towers", fileNameKey: nameof(TowerTemplatePrefabKey))]
    public GameObject towerTemplate;

    public string TowerBaseSpriteKey;
    [AssetFromFile("Items/Towers/Sprites", ".png", fileNameKey: nameof(TowerBaseSpriteKey))]
    public Sprite TowerBaseSprite;
    public Vector2 TowerBaseSpriteOffset;

    public string TowerHeadSpriteKey;
    [AssetFromFile("Items/Towers/Sprites", ".png", fileNameKey: nameof(TowerHeadSpriteKey))]
    public Sprite TowerHeadSprite;
    public Vector2 TowerHeadSpriteOffset;
}
```

* JSON belongs to: `Mods/<ModName>/Items/Towers/*.json`
* Tower sprites: `Mods/<ModName>/Items/Towers/Sprites/<SpriteKey>.png`
* Template prefab (base game): `Assets/Resources/Prefabs/Towers/<TowerTemplatePrefabKey>.prefab`

---

## 4) Authoring a mod: **Add a new Tower** (step-by-step)

This is a concrete walkthrough to create the **Ballistic Turret** from your example.

### Step 1 — Prepare the mod folder

Create:

```
Mods/
└─ MyFirstMod/
   └─ Items/
      ├─ Icons/
      │  └─ Turret01.png
      └─ Towers/
         ├─ towers.json
         └─ Sprites/
            ├─ Tower_BallisticTurret_Base.png
            └─ Tower_BallisticTurret_Head.png
```

**Important:**

* **Filenames must match the keys** you’ll put in JSON:

  * `IconKey: "Turret01"` → `Items/Icons/Turret01.png`
  * `TowerBaseSpriteKey: "Tower_BallisticTurret_Base"` → `Items/Towers/Sprites/Tower_BallisticTurret_Base.png`
  * `TowerHeadSpriteKey: "Tower_BallisticTurret_Head"` → `Items/Towers/Sprites/Tower_BallisticTurret_Head.png`

### Step 2 — Reference a template prefab (base game)

Your tower uses a **template** that ships with the game:

* `TowerTemplatePrefabKey: "TurretBaseTemplate"` must exist at:

  ```
  Assets/Resources/Prefabs/Towers/TurretBaseTemplate.prefab
  ```
* That prefab **must** include a `TowerView` component with:

  ```csharp
  void UpdateTowerGraphics(Sprite baseSprite, Sprite headSprite,
                           Vector2 baseOffset, Vector2 headOffset)
  ```

  The factory will call this.

### Step 3 — Write the JSON

`Mods/MyFirstMod/Items/Towers/towers.json`:

```json
[
  {
    "id": "Tower_BallisticTurret",
    "displayName": "Ballistic Turret",
    "description": "Tastes bland",
    "iconKey": "Turret01",

    "towerTemplatePrefabKey": "TurretBaseTemplate",
    "Price": 1,

    "attackRadius": 4,
    "cooldown": 1,

    "projectilePrefabKey": "baseProjectile",

    "towerBaseSpriteKey": "Tower_BallisticTurret_Base",
    "towerBaseSpriteOffset": { "x": 0.0, "y": 0.0 },

    "towerHeadSpriteKey": "Tower_BallisticTurret_Head",
    "towerHeadSpriteOffset": { "x": 0.203, "y": 0.0 },

    "effects": [
      { "effectType": "BuildTower",
        "effectParams": { "towerID": "Tower_BallisticTurret" }
      }
    ]
  }
]
```

**Notes**

* **Key casing is treated case-insensitively** by your loader (your example mixes `Price` and `attackRadius` casing).
* **Id must be unique** across all mods.
* `projectilePrefabKey` must map to something your projectile system can resolve (either via your own `[AssetFromFile]` on `ProjectilePrefab`, or a separate lookup system).

### Step 4 — Ensure assets exist

* `Items/Icons/Turret01.png` (icon)
* `Items/Towers/Sprites/Tower_BallisticTurret_Base.png`
* `Items/Towers/Sprites/Tower_BallisticTurret_Head.png`
* Base game: `Resources/Prefabs/Towers/TurretBaseTemplate.prefab` with `TowerView`.

> **Tip:** For sprites, normal Unity sprite import settings (Sprite Mode = Single, Alpha is Transparency, etc.) are usually fine.

### Step 5 — Launch the game and check the Mod Manager

* Place `MyFirstMod/` under the mods root folder the game uses.
* In the **Mod Manager**, set the **load order** you want.
* Start a game/session. The loader will:

  * Discover your JSON via `[ContentFolder("Items/Towers")]`
  * Parse it into a `TowerDef` ScriptableObject
  * Fill its sprite/prefab fields via `[AssetFromFile(...)]`

### Step 6 — Runtime spawning (developer tools / debug)

At runtime you can spawn your tower by ID:

```csharp
RuntimeObjects.TrySpawn<TowerDef>(
    "Tower_BallisticTurret",
    pos,
    Quaternion.FromToRotation(Vector3.up, normal),
    out var go);
```

If it returns `false`, the ID wasn’t found in the `ContentCatalogue` (bad load order, missing JSON, typo in `id`, etc.).

---

## 5) How the runtime builds the tower

### 5.1 Factory registration & warmup

You have a generic factory registry:

```csharp
public static class RuntimeFactoryRegistry
{
    public static void Register<TDef>(IRuntimeFactory<TDef> f) where TDef : IContentDef
        => _byDefType[typeof(TDef)] = f;

    public static IRuntimeFactory<TDef> Get<TDef>() where TDef : IContentDef
        => (IRuntimeFactory<TDef>)_byDefType[typeof(TDef)];
}
```

**Register your factory** during startup (once):

```csharp
public sealed class GameplayBootstrap : MonoBehaviour
{
    void Awake()
    {
        RuntimeFactoryRegistry.Register(new TowerFactory());

        // Optional warmup: validate & cache all towers
        foreach (var def in ContentCatalogue.Instance.All<TowerDef>())
        {
            RuntimeFactoryRegistry.Get<TowerDef>().Setup(def);
        }
    }
}
```

### 5.2 The `TowerFactory`

```csharp
public sealed class TowerFactory : IRuntimeFactory<TowerDef>
{
    private readonly Dictionary<string, GameObject> _prefabCache = new();

    public void Setup(TowerDef def)         => GetOrBakePrefab(def);
    public GameObject Build(TowerDef def, Transform parent = null)
        => Object.Instantiate(GetOrBakePrefab(def), parent);

    private GameObject GetOrBakePrefab(TowerDef def)
    {
        if (_prefabCache.TryGetValue(def.Id, out var cached)) return cached;

        if (def.towerTemplate == null)
            throw new System.Exception($"Tower '{def.Id}' is missing TemplatePrefab");

        var baked = Object.Instantiate(def.towerTemplate);
        baked.name = $"Tower[{def.Id}]";

        if (!baked.TryGetComponent<TowerView>(out var view))
            throw new System.Exception("Tower view is missing on template");
        else
            view.UpdateTowerGraphics(
                def.TowerBaseSprite, def.TowerHeadSprite,
                def.TowerBaseSpriteOffset, def.TowerHeadSpriteOffset);

        baked.SetActive(false);
        _prefabCache[def.Id] = baked;
        return baked;
    }
}
```

* `Setup(def)` **validates** and **pre-bakes** a prefab per tower (fast spawning later).
* `Build(def)` returns a live instance cloned from the baked prefab.
* **Required components**: the template **must** have `TowerView`; otherwise you’ll get `"Tower view is missing on template"`.

### 5.3 Spawning API

```csharp
public static class RuntimeObjects
{
    public static GameObject Spawn<TDef>(string id, Vector3 pos, Quaternion rot)
        where TDef : class, IContentDef
    {
        if (!ContentCatalogue.Instance.TryGet<TDef>(id, out var def))
            throw new Exception($"Def '{id}' ({typeof(TDef).Name}) nicht gefunden");

        var go = RuntimeFactoryRegistry.Get<TDef>().Build(def);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public static bool TrySpawn<TDef>(string id, Vector3 pos, Quaternion rot, out GameObject go)
        where TDef : class, IContentDef
    {
        go = null;
        if (!ContentCatalogue.Instance.TryGet<TDef>(id, out var def)) return false;
        go = RuntimeFactoryRegistry.Get<TDef>().Build(def);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return true;
    }
}
```

---

## 6) Field-by-field: `TowerDef` JSON schema

| JSON key                 | Unity field                              | Type       | Source/Resolution                                               | Required | Example                        |
| ------------------------ | ---------------------------------------- | ---------- | --------------------------------------------------------------- | -------- | ------------------------------ |
| `id`                     | `Id`                                     | string     | Unique content ID (case-insensitive)                            | **Yes**  | `"Tower_BallisticTurret"`      |
| `displayName`            | `DisplayName`                            | string     | Shown in UI, inventories                                        | Yes      | `"Ballistic Turret"`           |
| `description`            | `Description`                            | string     | Tooltip or UI description                                       | Optional | `"Tastes bland"`               |
| `iconKey`                | `IconKey` + `Icon`                       | Sprite     | `Mods/<Mod>/Items/Icons/<IconKey>.png`                          | Yes      | `"Turret01"` → `Turret01.png`  |
| `Price`                  | `Price`                                  | int        | Cost in your economy                                            | Yes      | `1`                            |
| `attackRadius`           | `AttackRadius`                           | float      | Gameplay                                                        | Yes      | `4`                            |
| `cooldown`               | `Cooldown`                               | float      | Gameplay                                                        | Yes      | `1`                            |
| `projectilePrefabKey`    | `ProjectilePrefabKey`                    | string     | Resolved by your projectile system                              | If used  | `"baseProjectile"`             |
| (n/a)                    | `ProjectilePrefab`                       | GameObject | If you bind this, add an `[AssetFromFile]` or separate resolver | If used  |                                |
| `towerTemplatePrefabKey` | `TowerTemplatePrefabKey`+`towerTemplate` | GameObject | `Assets/Resources/Prefabs/Towers/<Key>.prefab`                  | **Yes**  | `"TurretBaseTemplate"`         |
| `towerBaseSpriteKey`     | `TowerBaseSpriteKey`+`TowerBaseSprite`   | Sprite     | `Mods/<Mod>/Items/Towers/Sprites/<Key>.png`                     | **Yes**  | `"Tower_BallisticTurret_Base"` |
| `towerBaseSpriteOffset`  | `TowerBaseSpriteOffset`                  | Vector2    | `{ "x": float, "y": float }`                                    | Optional | `{ "x": 0.0, "y": 0.0 }`       |
| `towerHeadSpriteKey`     | `TowerHeadSpriteKey`+`TowerHeadSprite`   | Sprite     | `Mods/<Mod>/Items/Towers/Sprites/<Key>.png`                     | **Yes**  | `"Tower_BallisticTurret_Head"` |
| `towerHeadSpriteOffset`  | `TowerHeadSpriteOffset`                  | Vector2    | `{ "x": float, "y": float }`                                    | Optional | `{ "x": 0.203, "y": 0.0 }`     |
| `effects`                | `Effects`                                | list       | Interpreted by your effect system (e.g., `BuildTower`)          | Optional | see example JSON               |

> **Casing:** Your loader treats keys case-insensitively, so `"Price"` and `"price"` both map. Stick to one style for sanity.

---

## 7) Troubleshooting & common errors

| Symptom / Error message                                   | Likely cause & fix                                                                                               |
| --------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `Def 'Tower_BallisticTurret' (TowerDef) nicht gefunden`   | Wrong `id` in spawn call, JSON didn’t load, or wrong load order. Verify `towers.json` path & Mod Manager order.  |
| `Tower 'Tower_BallisticTurret' is missing TemplatePrefab` | `towerTemplate` not resolved. Check `towerTemplatePrefabKey` matches a prefab under `Resources/Prefabs/Towers/`. |
| `Tower view is missing on template`                       | Template prefab lacks `TowerView` component. Add it and ensure `UpdateTowerGraphics(...)` exists.                |
| Icon or sprite missing visually                           | Key-to-file mismatch. Check file exists at the exact mod path and filename (`.png`) matches the JSON key.        |
| Two mods define the same `id`                             | Undefined which one “wins.” Use unique IDs across all mods to avoid conflicts.                                   |
| Projectile doesn’t fire                                   | `projectilePrefabKey` isn’t resolved by your gameplay systems. Verify mapping and that the prefab exists.        |
| Offsets look wrong                                        | Adjust `towerBaseSpriteOffset` / `towerHeadSpriteOffset` in JSON and re-test.                                    |

---

## 8) Best practices for mod authors

* **Unique IDs**: Prefix with your mod name, e.g., `MyMod_Tower_BallisticTurret`.
* **Consistent naming**: Keep JSON keys and filenames aligned exactly.
* **Bundle only what you own**: Game-shipped templates go in `Resources` (not in the mod). Modders supply sprites, icons, their JSON.
* **Organize JSON**: You can split into multiple files under `Items/Towers/` if helpful; the loader reads all of them.

---

## 9) Adding a **brand-new content type** (developers)

Suppose you want a new type, e.g., a **Trap**.

### Step A — Define the ScriptableObject

```csharp
[ContentFolder("Items/Traps")]
[CreateAssetMenu(menuName = "Defs/Item - Trap")]
public class TrapDef : ItemDef
{
    public float TriggerRadius;
    public float Cooldown;

    public string TemplateKey;
    [AssetFromFile("Resources:Prefabs/Traps", fileNameKey: nameof(TemplateKey))]
    public GameObject Template;

    public string SpriteKey;
    [AssetFromFile("Items/Traps/Sprites", ".png", fileNameKey: nameof(SpriteKey))]
    public Sprite Sprite;

    public Vector2 SpriteOffset;
}
```

### Step B — Implement a runtime factory

```csharp
public sealed class TrapFactory : IRuntimeFactory<TrapDef>
{
    private readonly Dictionary<string, GameObject> _cache = new();

    public void Setup(TrapDef def) => GetOrBake(def);

    public GameObject Build(TrapDef def, Transform parent = null)
        => Object.Instantiate(GetOrBake(def), parent);

    private GameObject GetOrBake(TrapDef def)
    {
        if (_cache.TryGetValue(def.Id, out var go)) return go;

        if (def.Template == null)
            throw new Exception($"Trap '{def.Id}' is missing Template");

        var baked = Object.Instantiate(def.Template);
        baked.name = $"Trap[{def.Id}]";

        // Example: apply visuals via your trap view
        if (baked.TryGetComponent<TrapView>(out var view))
            view.ApplySprite(def.Sprite, def.SpriteOffset);

        baked.SetActive(false);
        _cache[def.Id] = baked;
        return baked;
    }
}
```

### Step C — Register and warm up

```csharp
RuntimeFactoryRegistry.Register(new TrapFactory());
foreach (var def in ContentCatalogue.Instance.All<TrapDef>())
    RuntimeFactoryRegistry.Get<TrapDef>().Setup(def);
```

### Step D — Author JSON + assets in a mod

```
Mods/MyTrapMod/Items/Traps/traps.json
Mods/MyTrapMod/Items/Traps/Sprites/<SpriteKey>.png
```

`traps.json` (example):

```json
[
  {
    "id": "MyMod_Trap_Spike",
    "displayName": "Spike Trap",
    "description": "Unfriendly floor decor",
    "iconKey": "TrapIcon01",
    "price": 10,

    "triggerRadius": 1.5,
    "cooldown": 2.0,

    "templateKey": "SpikeTrapTemplate",
    "spriteKey": "SpikeTrapSprite",
    "spriteOffset": { "x": 0.0, "y": 0.0 }
  }
]
```

Spawn it with:

```csharp
RuntimeObjects.Spawn<TrapDef>("MyMod_Trap_Spike", pos, rot);
```

---

## 10) Developer checklists

### Loader & content catalogue

* [ ] Each `ContentDef` has `[ContentFolder("...")]`.
* [ ] JSON → `Id` unique (case-insensitive).
* [ ] `SourceFile` set by loader for debugging.

### Asset binding

* [ ] Each asset field that depends on a key has `[AssetFromFile(..., fileNameKey: nameof(KeyField))]`.
* [ ] For `Resources:` assets, confirm the prefab exists under `Assets/Resources/...`.
* [ ] For mod assets, confirm files exist at `Mods/<Mod>/<Path>/<Key>.<ext>`.

### Runtime factories

* [ ] There is exactly one `IRuntimeFactory<TDef>` per content type.
* [ ] Register factory at startup.
* [ ] Call `Setup(def)` for all defs (validation + warm cache).
* [ ] `Build(def)` never mutates shared state; it instantiates from a cached baked prefab.

### Spawning

* [ ] Use `TrySpawn<TDef>(id, ...)` in gameplay code where failure is non-fatal.
* [ ] Use `Spawn<TDef>(id, ...)` when failure should throw.

---

## 11) FAQ & subtle details

* **Are JSON keys case-sensitive?**
  Your pipeline treats them **case-insensitively** (your sample mixes cases). Pick a style and stick to it.

* **Where does `SourceFile` come from?**
  The loader sets it to the path of the JSON file that created the def. It’s great for debugging errors.

* **What if two mods define the same `id`?**
  Don’t do that; IDs must be unique globally. If it happens, behavior depends on the catalogue/loader. Avoid by namespacing IDs: `ModName_Type_Name`.

* **How do I handle prefabs not in `Resources`?**
  For mod-supplied prefabs, add your own `[AssetFromFile]` pointing to a mod path (e.g., `Items/Towers/Prefabs`) and reference by key in JSON.

* **Can I split large JSON files?**
  Yes. The loader reads **all** JSON files in the `[ContentFolder]` path, so you can have one JSON per item/tower if you prefer.

---

## 12) Quick smoke test (copy-paste)

1. Create `Mods/MyFirstMod/Items/Towers/towers.json` with the Ballistic Turret JSON above.
2. Add:

   * `Items/Icons/Turret01.png`
   * `Items/Towers/Sprites/Tower_BallisticTurret_Base.png`
   * `Items/Towers/Sprites/Tower_BallisticTurret_Head.png`
3. Ensure base game contains: `Resources/Prefabs/Towers/TurretBaseTemplate.prefab` with `TowerView`.
4. Verify your bootstrap registers `TowerFactory` and warms up.
5. Run the game. In Mod Manager, enable the mod and confirm load order.
6. In a test script, call:

   ```csharp
   RuntimeObjects.TrySpawn<TowerDef>("Tower_BallisticTurret", somePos, Quaternion.identity, out _);
   ```

   You should see a live tower with the correct base/head sprites and offsets.

---

## 13) Optional enhancements

* **Content validation phase**: after load (before entering gameplay), iterate all defs and:

  * Check missing assets, illegal ranges, empty names.
  * Report with `def.SourceFile` and line/identifier hints.
* **Editor tooling**: add a menu item to generate a JSON stub from a selected `ContentDef` asset.
* **Hot reload**: watch mod folders for file changes, re-load JSON for rapid iteration (keep factories’ caches in sync).

---

That’s the complete pipeline: put JSON + assets in the right folder, ensure keys match filenames, rely on `[AssetFromFile]` to bind, let the loader fill `ContentDef`s, have a factory bake and instantiate, and use `RuntimeObjects` to spawn by ID. If you’d like, I can also produce a JSON template generator or a unit test that validates every loaded def before play.
