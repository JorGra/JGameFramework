using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>
    /// Shared colour palette that can be referenced by multiple ThemeAsset instances.
    /// </summary>
    [CreateAssetMenu(menuName = "UI/Theme/Color Palette", fileName = "ThemeColorPalette")]
    public sealed class ThemeColorPalette : ScriptableObject
    {
        [Serializable]
        struct ColorEntry
        {
            public string key;
            public Color color;
        }

        [SerializeField] private List<ColorEntry> colors = new();

        public bool TryGetColor(string key, out Color color)
        {
            for (int i = 0; i < colors.Count; i++)
            {
                if (colors[i].key == key)
                {
                    color = colors[i].color;
                    return true;
                }
            }

            color = default;
            return false;
        }
    }
}
