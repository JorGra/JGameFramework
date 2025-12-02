# Quick guide: Add a new content type via code

Goal: Introduce a brand-new type (e.g., “Trap”) so mods can supply JSON+assets and the game can spawn instances at runtime.

## 1) Define the data object

* Create a `ContentDef` class (optionally derive from `ItemDef`).
* Point to the mod JSON folder with `[ContentFolder("…")]`.
* Bind assets with `[AssetFromFile(..., fileNameKey: nameof(Key))]`.
* Every def needs a **unique** `Id` (case-insensitive).

```csharp
[ContentFolder("Items/Traps")]
[CreateAssetMenu(menuName = "Defs/Item - Trap")]
public class TrapDef : ItemDef
{
    public float TriggerRadius, Cooldown;

    public string TemplateKey;
    [AssetFromFile("Resources:Prefabs/Traps", fileNameKey: nameof(TemplateKey))]
    public GameObject Template;

    public string SpriteKey;
    [AssetFromFile("Items/Traps/Sprites", ".png", fileNameKey: nameof(SpriteKey))]
    public Sprite Sprite;

    public Vector2 SpriteOffset;
}
```

## 2) Implement the runtime factory

* Exactly one `IRuntimeFactory<TDef>` per type.
* `Setup(def)`: validate + bake prefab into a cache.
* `Build(def)`: instantiate from the cached prefab.

```csharp
public sealed class TrapFactory : IRuntimeFactory<TrapDef>
{
    private readonly Dictionary<string, GameObject> _cache = new();

    public void Setup(TrapDef def) => GetOrBake(def);
    public GameObject Build(TrapDef def, Transform parent = null)
        => Object.Instantiate(GetOrBake(def), parent);

    GameObject GetOrBake(TrapDef def)
    {
        if (_cache.TryGetValue(def.Id, out var go)) return go;
        if (def.Template == null) throw new Exception($"Trap '{def.Id}' missing Template");

        var baked = Object.Instantiate(def.Template);
        baked.name = $"Trap[{def.Id}]";

        if (baked.TryGetComponent<TrapView>(out var view))
            view.ApplySprite(def.Sprite, def.SpriteOffset);

        baked.SetActive(false);
        _cache[def.Id] = baked;
        return baked;
    }
}
```

## 3) Register & warm up (startup)

```csharp
void Awake()
{
    RuntimeFactoryRegistry.Register(new TrapFactory());
    foreach (var def in ContentCatalogue.Instance.All<TrapDef>())
        RuntimeFactoryRegistry.Get<TrapDef>().Setup(def);
}
```

## 4) Mod JSON & assets

* JSON lives under the path declared in `[ContentFolder]` inside each mod.
* Keys in JSON resolve to files in the mod folder or under `Resources:` in the base game.

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

Files:

```
Mods/MyTrapMod/Items/Traps/traps.json
Mods/MyTrapMod/Items/Traps/Sprites/SpikeTrapSprite.png
Assets/Resources/Prefabs/Traps/SpikeTrapTemplate.prefab   (base game)
```

## 5) Spawn at runtime

```csharp
RuntimeObjects.Spawn<TrapDef>("MyMod_Trap_Spike", pos, rot);
// or
RuntimeObjects.TrySpawn<TrapDef>("MyMod_Trap_Spike", pos, rot, out var go);
```

## 6) Minimal checklist

* `[ContentFolder]` set; JSON placed accordingly.
* `Id` unique; loader sets `SourceFile` automatically.
* `[AssetFromFile]` paths/keys match existing files.
* Required `Resources/...` prefabs exist for `Resources:` lookups.
* Factory registered and `Setup` run for all defs.

This is all you need to add a new content type, load it from mods, and spawn it in-game.
