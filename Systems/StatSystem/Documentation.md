# Runtime-JSON Stat System

A lightweight, fully runtime-driven stats framework. All definitions come from a single JSON (`stats.json`) in **Resources**. No SO profiles or static lookups.

---

## 1. JSON Format

Place `StatDefinitions.json` under `Assets/Resources/StatDefinitions/`:

```json
{
  "stats": [
    { "key": "power",     "statName": "Power",     "defaultValue": 0.0 },
    { "key": "knowledge", "statName": "Knowledge", "defaultValue": 0.0 },
    { "key": "skill",     "statName": "Skill",     "defaultValue": 0.0 }
  ]
}
```

---

## 2. Bootstrap

1. **StatRegistry**  
   Holds all `StatDefinition`s loaded from JSON
2. **StatRegistryProvider**  
   Singleton that reads the JSON on `Awake()` via `Resources.Load<TextAsset>` and calls  
   `registry.InitializeFromJsonText(ta.text)` 

```csharp
// In your scene:
var registry = StatRegistryProvider.Instance.Registry;
```

---

## 3. Accessing Stats

```csharp
// Construct per-entity Stats container (loads defaults):
var stats = new Stats();  // uses StatRegistry defaults 

// Lookup a definition by key:
var powerDef = registry.Get("power");

// Query final value (base + modifiers):
float currentPower = stats.GetStat(powerDef);
```

---

## 4. Modifiers

### Direct

```csharp
// Create and add a +10 buff for 5s:
var mod = new StatModifier(powerDef, new AddOperation(10f), 5f);
stats.Mediator.AddModifier(mod);
```

### Factory

```csharp
// Use IStatModifierFactory for decoupling:
IStatModifierFactory factory = new StatModifierFactory(); 
var permBoost = factory.Create(powerDef, OperatorType.Add, 20f, 0f);
stats.Mediator.AddModifier(permBoost);
```

Each frame, tick durations and clean up:

```csharp
void Update() => stats.Mediator.Update(Time.deltaTime);
```

---

## 5. StatDefinition

```csharp
// Defines the shape of each stat loaded from JSON:
[CreateAssetMenu]
public class StatDefinition : ScriptableObject
{
    public string key;
    public string statName;
    public float defaultValue;
}
