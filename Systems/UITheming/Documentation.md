# Unified Theme System — Complete Guide (Typed-Sheet Architecture)

---

## ➊ System Overview

A **Theme** is a single `ThemeAsset` that packs every visual resource your UI may need: colour swatches, sprites, fonts and any number of **typed style-sheets**.
Each sheet is a small inline object that stores a list of *one* concrete style class (for example, `TextStyleParameters`).
At runtime, **ThemeManager** keeps the active theme and notifies all components that implement **`IThemeable`** so they can refresh their visuals.

Key runtime classes

* **ThemeAsset** — root ScriptableObject. It references a shared colour palette, optional overrides, sprite / font lists and an array of `StyleSheetBase` items, plus an optional `baseTheme` for single-level inheritance .
* **ThemeColorPalette** — swatch collection (`Primary`, `Accent`, `OnDanger`…) that keeps colour changes centralised across multiple themes.
* **StyleSheetBase** ➜ **StyleSheet<T>** — generic wrappers that hold a typed list (`List<T>`). Unity serialises them inside the asset; no extra files needed.
* **StyleModuleParameters** — base class for every style entry. It only stores the public `StyleKey` string .
* **ThemeManager** — lightweight singleton; swaps themes and raises `ThemeChangedEvent` through your existing `EventBus` .
* **IThemeable** — contract implemented by UI behaviours that react to theme changes .
* **ThemeOverride** — optional helper that lets a Canvas (or any transform) use a different theme locally .

---

## ➋ Designer Workflow (no code)

### 1. Create a ThemeAsset

```
Assets ▸ Create ▸ UI ▸ Theme Asset
```

Give it a descriptive name such as **“LightTheme”**.

### 2. Fill the base resources

1. Open the asset in the Inspector.
2. Assign a **Theme Color Palette** (Create ➜ UI ➜ Theme ➜ Color Palette) to centralise swatches.
3. Under **Colour Overrides**, **Sprites** and **Fonts** press **+** to add rows.
4. Type a *unique* **key** (e.g. `Primary`, `Icon/Close`, `Bold`) and assign the value (colour picker, sprite reference, TMP font asset). Overrides replace palette entries with matching keys.

### 3. Add typed style-sheets

1. Expand **Style Sheets** and click **Add ▼**.
2. Pick one of the available sheets, e.g. **Text Style Sheet**.
3. Inside that sheet click **+** to add individual style entries, then fill their fields.

   * Most fields reference the keys you defined in the base resources step, keeping everything data-driven.
   * Every key field now exposes a picker button (`...`) with live previews for colours, sprites, fonts and styles, so you never have to copy string tokens by hand.
4. Repeat for every sheet you need (Toggle, Image, etc.).

   * Only unused sheets are offered in the **Add ▼** menu, so the list stays tidy.

### 4. Inheritance (optional)

If you want a theme that tweaks just a few values:

1. Duplicate your existing theme asset, name it e.g. **“LightTheme\_HighContrast”**.
2. In the child asset, set **Base Theme** to the original.
3. Add or edit only the keys you need to override.

   * Overriding entries are marked with a green dot by the custom inspector.

### 5. Scene setup

1. Drop a **ThemeManager** in the first scene that shows UI.
2. Assign the desired ThemeAsset to **Default Theme**.
3. Add `Themeable…` components to your UI prefabs (e.g. `ThemeableText`, `ThemeableButton`, `ThemeableToggle`).
4. Set each component’s **Style Key** so it knows which entry to pull from the theme (Primary, Secondary, Ghost…).
5. Play — the elements will immediately style themselves and will update live if you call `ThemeManager.SetTheme(newAsset)` at runtime.

That’s it — no prefabs, no code editing, just keys and references in the Inspector.

---

## ➌ Coder Workflow

### A. Adding a brand-new styleable component

Suppose you want to theme a **Slider**.

1. **Create the parameters class** (one per visual style).

   ```csharp
   // SliderStyleParameters.cs
   using System;
   using UnityEngine;

   [Serializable]
   public sealed class SliderStyleParameters : StyleModuleParameters
   {
       [SerializeField] string fillSpriteKey    = "";
       [SerializeField] string handleSpriteKey  = "";
       [SerializeField] string backgroundColorKey = "Background";

       public string FillSpriteKey    => fillSpriteKey;
       public string HandleSpriteKey  => handleSpriteKey;
       public string BackgroundColorKey => backgroundColorKey;
   }
   ```

2. **Create the sheet** (one line):

   ```csharp
   [Serializable]
   public sealed class SliderStyleSheet : StyleSheet<SliderStyleParameters> { }
   ```

3. **Write the themeable behaviour**:

   ```csharp
   [RequireComponent(typeof(Slider))]
   public sealed class ThemeableSlider : MonoBehaviour, IThemeable
   {
       [SerializeField] string styleKey = "Slider";

        Slider slider;
        Image background;
        Image fill;
        Image handle;
        EventSubscription<ThemeChangedEvent> subscription;

        void Awake()
        {
            slider     = GetComponent<Slider>();
            background = slider.GetComponent<Image>();
            fill       = slider.fillRect.GetComponent<Image>();
            handle     = slider.handleRect.GetComponent<Image>();
        }

        void OnEnable()
        {
            subscription = this.SubscribeEvent<ThemeChangedEvent>(e => ApplyTheme(e.Theme));
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        void OnDisable()
        {
            subscription?.Dispose();
            subscription = null;
        }

       public void ApplyTheme(ThemeAsset theme)
       {
           if (!theme.TryGetStyle(styleKey, out SliderStyleParameters s)) return;

           background.color = theme.GetColor(s.BackgroundColorKey);
           fill.sprite      = theme.GetSprite(s.FillSpriteKey);
           handle.sprite    = theme.GetSprite(s.HandleSpriteKey);
       }
   }
   ```

4. **Designer actions**

   * Add **Slider Style Sheet** to the ThemeAsset, create a “Slider” entry and set the sprite/colour keys.
   * Attach `ThemeableSlider` to UI sliders and set **Style Key** to `Slider`.

No other code needs to change — the new sheet automatically appears in the **Add ▼** menu of every ThemeAsset.

### B. Authoring animated buttons (without Animator controllers)

1. In your ThemeAsset add or open the **Button Style Sheet** and create entries such as `Button/Primary`, `Button/Secondary`.
2. Configure the shared label style and animation settings (duration, easing, scaled vs. unscaled time).
3. For each interaction state (Normal, Highlighted, Pressed, Selected, Disabled) fill the fields you want to drive:
   * Reference palette keys for background and label colours.
   * Toggle **Animate Scale** or **Animate Size Delta** to pulse or squash the button on hover/press.
   * Use **Label Style Override** when a state needs a distinct text preset (e.g. all‑caps focus variant).
   * Enable **Include Icon** if your prefab has a companion graphic, fill its colour keys, and assign the icon reference on `ThemeableButton` so it animates with the label.
4. Drop `ThemeableButton` on your button prefab, assign the same **Style Key**, and optionally point `Icon` to the graphic you want tinted alongside the label (plus override `TMP_Text` / `RectTransform` if the defaults do not fit).
5. Play — the component animates between states with a single data source, no Animator controller required.

Changing a colour in the shared palette (e.g. updating `Primary`) immediately updates every themed button variant because each state resolves colours through the new `ThemeColorPalette` reference.

### C. Expanding an existing style type

Need an outline colour for text?

1. Open `TextStyleParameters` and add:

   ```csharp
   [SerializeField] string outlineColorKey = "Outline";
   public string OutlineColorKey => outlineColorKey;
   ```

2. In `ThemeableText.ApplyTheme`:

   ```csharp
   text.outlineColor = theme.GetColor(style.OutlineColorKey);
   ```

3. Designers edit existing Text entries in the asset and set the new field. Old assets stay valid because Unity ignores unknown fields until they appear in code.

### D. Performance tweak (optional)

If your lists grow into hundreds of entries, add a dictionary cache in `ThemeAsset.OnEnable()`; all public APIs remain unchanged.

---

## ➍ Key Naming Tips

* Use **semantic tokens** (`Primary`, `Error`) for colours.
* Reference **asset paths** (`Icon/Play`, `UI/CheckboxChecked`) for sprites.
* Keep **Style Keys** short and consistent (`Body`, `Button/Primary`, `Slider/Volume`).
* Avoid spaces; stick to PascalCase or slash-separated hierarchies.

---

## ➎ Quick Runtime Snippets

```csharp
// Colour, sprite, font
Color  c = theme.GetColor ("Primary");
Sprite s = theme.GetSprite("Icon/Close");
var    f = theme.GetFont  ("Bold");

// Style look-up
if (theme.TryGetStyle("Body", out TextStyleParameters body))
{
    label.color    = theme.GetColor(body.ColorKey);
    label.fontSize = body.FontSize;
}

// Hot-swap theme at runtime
ThemeManager.Instance.SetTheme(nightTheme);
```

---

### You’re ready

With typed style-sheets, one `ThemeAsset` carries your entire look-and-feel, is trivial to extend, and stays organised even as the project grows. Designers focus on keys and asset references; coders add clear, modular classes when new UI elements appear. Happy theming!
