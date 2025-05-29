using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace UI.Theming
{
    /// <summary>
    /// ScriptableObject holding all visual data for a single UI theme.
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Theme Asset", fileName = "ThemeAsset")]
    public class ThemeAsset : ScriptableObject
    {
        [Header("Color Palette")]
        [SerializeField] private Color primaryColor = Color.white;
        [SerializeField] private Color secondaryColor = Color.gray;
        [SerializeField] private Color backgroundColor = Color.black;
        [SerializeField] private Color textColor = Color.white;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset defaultFont;

        [Header("Sprites")]
        [Tooltip("Keys must be unique and the same length as the Sprites array.")]
        [SerializeField] private string[] spriteKeys;
        [SerializeField] private Sprite[] sprites;

        // --- Public read-only properties -------------------------------------

        public Color PrimaryColor => primaryColor;
        public Color SecondaryColor => secondaryColor;
        public Color BackgroundColor => backgroundColor;
        public Color TextColor => textColor;
        public TMP_FontAsset DefaultFont => defaultFont;

        /// <summary>Look-up table generated on first access.</summary>
        private Dictionary<string, Sprite> spriteLookup;

        /// <summary>
        /// Attempts to fetch a sprite by key defined in the inspector.
        /// </summary>
        /// <param name="key">Unique sprite key.</param>
        /// <param name="sprite">Returned sprite.</param>
        /// <returns>True if found.</returns>
        public bool TryGetSprite(string key, out Sprite sprite)
        {
            EnsureLookUp();
            return spriteLookup.TryGetValue(key, out sprite);
        }

        // ---------------------------------------------------------------------

        void EnsureLookUp()
        {
            if (spriteLookup != null) return;

            spriteLookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            int count = Mathf.Min(spriteKeys?.Length ?? 0, sprites?.Length ?? 0);

            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(spriteKeys[i]) && sprites[i] != null)
                {
                    spriteLookup[spriteKeys[i]] = sprites[i];
                }
            }
        }
    }

    /// <summary>
    /// Identifies which color of the theme a component should use.
    /// </summary>
    public enum ColorRole
    {
        None,
        Primary,
        Secondary,
        Background,
        Text
    }
}
