using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>
    /// Single-file container for every resource a UI theme can expose
    /// (colours, sprites, fonts and strongly-typed style modules).
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Theme/Theme Asset", fileName = "Theme")]
    public sealed class ThemeAsset : ScriptableObject
    {
        #region – foundations –

        [Serializable]
        struct ColorSwatch { public string key; public Color color; }

        [Serializable]
        struct SpriteEntry { public string key; public Sprite sprite; }

        [Serializable]
        struct FontEntry { public string key; public TMP_FontAsset font; }

        [Header("Colours")]
        [SerializeField] private List<ColorSwatch> colors = new();

        [Header("Sprites")]
        [SerializeField] private List<SpriteEntry> sprites = new();

        [Header("Fonts")]
        [SerializeField] private List<FontEntry> fonts = new();

        #endregion

        #region – style modules –

        [Header("Styles")]
        [SerializeReference, SubclassSelector]
        private List<StyleModuleParameters> styles = new();

        #endregion

        // ------------------------------------------------------------
        // simple list look-ups (Option 4-3). < 100 entries is fine.
        // ------------------------------------------------------------

        /// <summary>Return a colour by key. White if missing.</summary>
        public Color GetColor(string key)
        {
            foreach (var c in colors)
                if (c.key == key) return c.color;

            return Color.white;
        }

        /// <summary>Return a sprite by key, or <c>null</c>.</summary>
        public Sprite GetSprite(string key)
        {
            foreach (var s in sprites)
                if (s.key == key) return s.sprite;

            return null;
        }

        /// <summary>Return a TMP font asset by weight/style key.</summary>
        public TMP_FontAsset GetFont(string key)
        {
            foreach (var f in fonts)
                if (f.key == key) return f.font;

            return null;
        }

        /// <summary>
        /// Try to get a <typeparamref name="T"/> style module with
        /// the requested <paramref name="styleKey"/>.
        /// </summary>
        public bool TryGetStyle<T>(string styleKey, out T style)
            where T : StyleModuleParameters
        {
            foreach (var s in styles)
                if (s is T typed && typed.StyleKey == styleKey)
                {
                    style = typed;
                    return true;
                }

            style = null;
            return false;
        }
    }
}
