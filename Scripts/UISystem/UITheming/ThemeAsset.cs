// ThemeAsset.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>
    /// Single-file container for every resource a UI theme can expose
    /// (colours, sprites, fonts and strongly-typed style modules).
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

        [Tooltip("Optional base-theme. Look-ups walk up this chain until the key is found.")]
        [SerializeField] private ThemeAsset baseTheme;

        [Header("Colours")][SerializeField] List<ColorSwatch> colors = new();
        [Header("Sprites")][SerializeField] List<SpriteEntry> sprites = new();
        [Header("Fonts")][SerializeField] List<FontEntry> fonts = new();

        // ───────────────────────────────────────────────────────────── style modules
        [Header("Styles")]
        [SerializeReference, SubclassSelector]
        List<StyleModuleParameters> styles = new();

        // ───────────────────────────────────────────────────────────── public API ──
        /// <summary>Return a colour by key. Throws if missing in entire chain.</summary>
        public Color GetColor(string key)
        {
            if (TryGetColor(key, out var c)) return c;

#if UNITY_EDITOR
            throw new KeyNotFoundException($"[ThemeAsset] Missing colour key '{key}' in '{name}' and its base chain.");
#else
            return Color.white; // fail-soft in builds
#endif
        }

        /// <summary>Return a sprite by key. Throws if missing.</summary>
        public Sprite GetSprite(string key)
        {
            if (TryGetSprite(key, out var s)) return s;

#if UNITY_EDITOR
            throw new KeyNotFoundException($"[ThemeAsset] Missing sprite key '{key}' in '{name}' and its base chain.");
#else
            return null;
#endif
        }

        /// <summary>Return a TMP font asset by key. Throws if missing.</summary>
        public TMP_FontAsset GetFont(string key)
        {
            if (TryGetFont(key, out var f)) return f;

#if UNITY_EDITOR
            throw new KeyNotFoundException($"[ThemeAsset] Missing font key '{key}' in '{name}' and its base chain.");
#else
            return null;
#endif
        }

        /// <summary>
        /// Try to get a style module with the requested <paramref name="styleKey"/>.
        /// </summary>
        public bool TryGetStyle<T>(string styleKey, out T style) where T : StyleModuleParameters
        {
            ThemeAsset current = this;
            HashSet<ThemeAsset> visited = null;

            while (current != null)
            {
                foreach (var s in current.styles)
                    if (s is T typed && typed.StyleKey == styleKey)
                    {
                        style = typed;
                        return true;
                    }

                // walk up
                visited ??= new HashSet<ThemeAsset>(4);
                if (!visited.Add(current))
                    break;                  // cycle protection

                current = current.baseTheme;
            }

            style = null;
            return false;
        }

        // ─────────────────────────────────────────────────────────── internal helpers
        bool TryGetColor(string key, out Color color)
        {
            ThemeAsset current = this;
            HashSet<ThemeAsset> visited = null;

            while (current != null)
            {
                foreach (var c in current.colors)
                    if (c.key == key)
                    {
                        color = c.color;
                        return true;
                    }

                // walk up
                visited ??= new HashSet<ThemeAsset>(4);
                if (!visited.Add(current))
                    break;

                current = current.baseTheme;
            }

            color = default;
            return false;
        }

        bool TryGetSprite(string key, out Sprite sprite)
        {
            ThemeAsset current = this;
            HashSet<ThemeAsset> visited = null;

            while (current != null)
            {
                foreach (var s in current.sprites)
                    if (s.key == key)
                    {
                        sprite = s.sprite;
                        return true;
                    }

                visited ??= new HashSet<ThemeAsset>(4);
                if (!visited.Add(current))
                    break;

                current = current.baseTheme;
            }

            sprite = null;
            return false;
        }

        bool TryGetFont(string key, out TMP_FontAsset font)
        {
            ThemeAsset current = this;
            HashSet<ThemeAsset> visited = null;

            while (current != null)
            {
                foreach (var f in current.fonts)
                    if (f.key == key)
                    {
                        font = f.font;
                        return true;
                    }

                visited ??= new HashSet<ThemeAsset>(4);
                if (!visited.Add(current))
                    break;

                current = current.baseTheme;
            }

            font = null;
            return false;
        }
    }
}
