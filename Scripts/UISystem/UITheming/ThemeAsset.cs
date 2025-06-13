// ThemeAsset.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [Serializable]
        private class SpriteEntry
        {
            public string key;
            public Sprite sprite;
            public Image.Type imageType = Image.Type.Sliced;
            [Min(1f)] public float pixelPerUnit = 1;
        }
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

        /// <summary>Return how the sprite should be drawn (Simple, Sliced…).</summary>
        public Image.Type GetSpriteType(string key) =>
            TryGetSpriteType(key, out var t) ? t : Image.Type.Simple;

        public float GetSpritePixelPerUnit(string key)
        {
            if (TryGetSpritePixelPerUnit(key, out var ppu))
            {
                return ppu;
            }
            throw new KeyNotFoundException($"[ThemeAsset] Missing sprite key '{key}' in '{name}' and its base chain.");
        }

        // ─────────────────────────────────────────────── internal linear look-ups ──
        bool TryGetColor(string key, out Color c) =>
            ScanList(this, t => t.colors, key, e => e.key, e => e.color, out c);

        bool TryGetSprite(string key, out Sprite s) =>
            ScanList(this, t => t.sprites, key, e => e.key, e => e.sprite, out s);

        bool TryGetFont(string key, out TMP_FontAsset f) =>
            ScanList(this, t => t.fonts, key, e => e.key, e => e.font, out f);

        bool TryGetSpriteType(string key, out Image.Type type) =>
            ScanList(this,               // start ThemeAsset
                     t => t.sprites,     // pick the sprite list on every hop
                     key,                // key we are looking for
                     e => e.key,         // how to read the key from a SpriteEntry
                     e => e.imageType,   // how to read the value we want
                     out type);          // out-parameter

        bool TryGetSpritePixelPerUnit(string key, out float multiplier) =>
            ScanList(this, t => t.sprites, key, e => e.key, e => e.pixelPerUnit, out multiplier);


        // Generic walker ----------------------------------------------------
        static bool ScanList<TEntry, TValue>(
                ThemeAsset start,
                Func<ThemeAsset, List<TEntry>> listSelector,
                string key,
                Func<TEntry, string> keyGetter,
                Func<TEntry, TValue> valueGetter,
                out TValue value)
        {
            ThemeAsset current = start;
            HashSet<ThemeAsset> visited = null;

            while (current != null)
            {
                var list = listSelector(current);
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (keyGetter(list[i]) == key)
                        {
                            value = valueGetter(list[i]);
                            return true;
                        }
                    }
                }

                visited ??= new HashSet<ThemeAsset>(4);
                if (!visited.Add(current)) break;          // cycle guard
                current = current.baseTheme;               // walk up
            }

            value = default;
            return false;
        }
    }
}
