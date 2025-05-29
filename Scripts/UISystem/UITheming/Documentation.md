# **Unified Theme System – Developer Reference**

---

## 1 · Purpose & Philosophy

This system provides **one self-contained `ThemeAsset`** that stores everything a UI theme might need—colours, sprites, fonts, and any number of strongly-typed *style modules*.
*Goals*:

* fast authoring (no ScriptableObject clutter)
* strongly typed look-ups (no error-prone string bags)
* drop-in extensibility (add new style types without touching existing code)

---

## 2 · Core Runtime Types

* **`ThemeAsset`** – single ScriptableObject storing:

  * *ColourSwatch* `List`   `key → Color`
  * *SpriteEntry* `List`    `key → Sprite`
  * *FontEntry* `List`      `key → TMP_FontAsset`
  * *Style modules* `List<StyleModuleParameters>` (polymorphic via `SerializeReference`)
* **`StyleModuleParameters`** – abstract base: every concrete style derives from this and must expose a **`StyleKey`** string.

  * Stock modules:

    * `TextStyleParameters`
    * `ImageStyleParameters`
    * `ToggleStyleParameters`
* **`ThemeManager`** (singleton) – holds the active `ThemeAsset` and broadcasts `ThemeChangedEvent` through your existing `EventBus`.
* **`IThemeable`** – interface for any component that wants to react to theme changes.

  * Stock implementations:

    * `ThemeableText` (TMP)
    * `ThemeableImage` (UGUI Image)
    * `ThemeableToggle` (UGUI Toggle)

---

## 3 · Authoring Workflow (Designer)

1. **Create a ThemeAsset**

   ```
   Assets ▸ Create ▸ UI ▸ Theme Asset
   ```
2. **Populate colour / sprite / font lists**
   *Press “+”* to add rows; set the **key** (unique string) and its value.
3. **Add style modules**

   * In the **Styles** fold-out click **➕ Add**.
   * A dropdown (from `SubclassSelector`) shows all concrete `StyleModuleParameters`.
   * After adding, fill out its fields—most reference the keys you defined above.
4. **Scene wiring**

   * Add **ThemeManager** to a scene and assign this asset as *Default Theme*.
   * Attach `Themeable…` behaviours to UI elements and type the **Style Key** that should drive them.

---

## 4 · Runtime Flow (Programmer)

```mermaid
graph TD
    ThemeManager(SetTheme or Awake)
    ThemeChanged(EventBus<ThemeChangedEvent>)
    Themeable[T≡ ThemeableText/Image/...]
    ThemeManager --> ThemeChanged
    ThemeChanged --> Themeable
    Themeable -->|ApplyTheme()| UI
```

* A call to `ThemeManager.Instance.SetTheme(asset)` swaps the theme.
* `ThemeManager` raises `ThemeChangedEvent`.
* Every enabled `Themeable…` receives the event, pulls its style module from the new `ThemeAsset` and applies values.

---

## 5 · Extending the System

### 5.1 Add a New Component Type

1. **Create a style module**

   ```csharp
   [Serializable]
   public sealed class SliderStyleParameters : StyleModuleParameters
   {
       [SerializeField] string fillSpriteKey = "";
       [SerializeField] string handleSpriteKey = "";
       [SerializeField] string backgroundColorKey = "Background";
       // expose public getters …
   }
   ```
2. **Create a Themeable behaviour**

   ```csharp
   [RequireComponent(typeof(Slider))]
   public sealed class ThemeableSlider : MonoBehaviour, IThemeable
   {
       [SerializeField] string styleKey = "Slider";
       /* cache ports in Awake … */

       public void ApplyTheme(ThemeAsset theme)
       {
           if (!theme.TryGetStyle(styleKey, out SliderStyleParameters s)) return;
           /* fetch sprites / colours from theme and assign */
       }
   }
   ```
3. **Use in Inspector**: add your new style module to the ThemeAsset, then attach `ThemeableSlider` to UI sliders and set *Style Key*.

### 5.2 Add a New Property to an Existing Module

*Open the module class*, add serialised field + getter, then update the matching `Themeable…` script to consume it.
No asset migrations needed—Unity serialisation preserves unknown fields.

---

## 6 · Best-practice Keys

* **Swatches** – semantic names (`Primary`, `Secondary`, `Danger`, …).
* **Fonts** – weight/style keys that match design tokens (`Regular`, `Bold`, `Italic`).
* **Style Keys** – entity + variant (`Body`, `Title/H1`, `Button/Primary`).
  Avoid spaces and keep the same casing across the project.

---

## 7 · Performance Notes

* Lists are scanned linearly (option 4-3); under \~100 entries each look-up is <0.1 µs.
* If you exceed that, convert lists to dictionaries in `ThemeAsset.OnEnable()`—no other code changes needed.

---

## 8 · Common Pitfalls

* **Missing keys** – look-ups silently fall back (`Color.white`, `null` sprite/font).
  *Tip*: enable *Strict Mode* in `ThemeAsset` by adding debug `Assert` calls when `TryGetStyle` fails.
* **Duplicate Style Keys** – the first match wins; keep them unique.
* **Disabled `Themeable` objects** – they register on *Enable*; call `ApplyTheme()` manually if you enable them after a theme swap.

---

## 9 · Quick API Cheatsheet

```csharp
// look-ups
Color            c = theme.GetColor("Primary");
Sprite           s = theme.GetSprite("IconClose");
TMP_FontAsset    f = theme.GetFont("Bold");

// style retrieval
if (theme.TryGetStyle("Body", out TextStyleParameters body)) { … }

// switch theme at runtime
ThemeManager.Instance.SetTheme(nightThemeAsset);
```

---

**You now have everything required** to create, apply, and extend themes in a single asset without clutter, while keeping the door open for future modules or performance tweaks. Happy theming!
