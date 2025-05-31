// ThemeAsset.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>
    /// Single-file container for every resource a UI theme can expose
    /// (colours, sprites, fonts and *typed* style sheets).
    /// Supports single-inheritance: assign <see cref="baseTheme"/> to
    /// override only the entries you need.
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Theme/Theme Asset", fileName = "Theme")]
    public sealed class ThemeAsset : ScriptableObject
    {
        // ───────────────────────────────────────────────────────────── foundations ─
        [Serializable] struct ColorSwatch { public string key; public Color color; }
        [Serializable] struct SpriteEntry { public string key; public Sprite sprite; }
        [Serializable] struct FontEntry { public string key; public TMP_FontAsset font; }

        [Tooltip("Optional base theme. Look-ups walk up this chain until the key is found.")]
        [SerializeField] private ThemeAsset baseTheme;

        [Header("Colours")][SerializeField] private List<ColorSwatch> colors = new();
        [Header("Sprites")][SerializeField] private List<SpriteEntry> sprites = new();
        [Header("Fonts")][SerializeField] private List<FontEntry> fonts = new();

        // ───────────────────────────────────────────────────────────── style sheets ─
        [Header("Style Sheets")]
        [SerializeReference, SubclassSelector]
        private List<StyleSheetBase> styleSheets = new();

        // ───────────────────────────────────────────────────────────── runtime API ──
        /// <summary>Return a colour by key; throws if missing in entire chain.</summary>
        public Color GetColor(string key) => TryGetColor(key, out var c)
            ? c
            : throw new KeyNotFoundException($"[ThemeAsset] Missing colour key '{key}' in '{name}' and its base chain.");

        /// <summary>Return a sprite by key; throws if missing.</summary>
        public Sprite GetSprite(string key) => TryGetSprite(key, out var s)
            ? s
            : throw new KeyNotFoundException($"[ThemeAsset] Missing sprite key '{key}' in '{name}' and its base chain.");

        /// <summary>Return a TMP font asset by key; throws if missing.</summary>
        public TMP_FontAsset GetFont(string key) => TryGetFont(key, out var f)
            ? f
            : throw new KeyNotFoundException($"[ThemeAsset] Missing font key '{key}' in '{name}' and its base chain.");

        /// <summary>
        /// Fetch a style of type <typeparamref name="T"/> and matching <paramref name="styleKey"/>.
        /// </summary>
        /// <typeparam name="T">Concrete style-parameter class.</typeparam>
        public bool TryGetStyle<T>(string styleKey, out T style) where T : StyleModuleParameters
        {
            ThemeAsset current = this;
            HashSet<ThemeAsset> visited = null;

            while (current != null)
            {
                // find the sheet that stores type T
                foreach (var sheet in current.styleSheets)
                {
                    if (sheet is StyleSheet<T> typedSheet &&
                        typedSheet.TryGet(styleKey, out style))
                    {
                        return true;
                    }
                }

                // walk up
                visited ??= new HashSet<ThemeAsset>(4);
                if (!visited.Add(current)) break;  // cycle-guard
                current = current.baseTheme;
            }

            style = null;
            return false;
        }

        // ─────────────────────────────────────────────── internal linear look-ups ──
        bool TryGetColor(string key, out Color color) => ScanList(colors, key, c => c.color, out color);
        bool TryGetSprite(string key, out Sprite spr) => ScanList(sprites, key, s => s.sprite, out spr);
        bool TryGetFont(string key, out TMP_FontAsset f) => ScanList(fonts, key, v => v.font, out f);

        // generic helper to cut duplication
        bool ScanList<TEntry, TValue>(List<TEntry> list, string key,
                                      Func<TEntry, TValue> valueGetter, out TValue value)
            where TEntry : struct
        {
            ThemeAsset current = this;
            HashSet<ThemeAsset> visited = null;

            while (current != null)
            {
                foreach (var entry in list)
                {
                    var k = entry.GetType().GetField("key")?.GetValue(entry) as string;
                    if (k == key)
                    {
                        value = valueGetter(entry);
                        return true;
                    }
                }

                visited ??= new HashSet<ThemeAsset>(4);
                if (!visited.Add(current)) break;
                current = current.baseTheme;
                list = current != null ? (List<TEntry>)typeof(ThemeAsset)
                        .GetField(list.GetType().Name.ToLower(), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.GetValue(current) : null;
            }

            value = default;
            return false;
        }
    }
}
