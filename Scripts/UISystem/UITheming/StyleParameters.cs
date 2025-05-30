// TextStyleParameters.cs
using System;
using TMPro;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>Visual definition for a TMP text element.</summary>
    [Serializable]
    public sealed class TextStyleParameters : StyleModuleParameters
    {
        [Header("Colour & Font")]
        [SerializeField] private string colorKey = "Text";
        [SerializeField] private string fontKey = "Regular";

        [Header("Typography")]
        [SerializeField] private int fontSize = 36;
        [SerializeField] private FontStyles fontStyle = FontStyles.Normal;

        public string ColorKey => colorKey;
        public string FontKey => fontKey;
        public int FontSize => fontSize;
        public FontStyles FontStyle => fontStyle;
    }

    /// <summary>Visual definition for a UI Image.</summary>
    [Serializable]
    public sealed class ImageStyleParameters : StyleModuleParameters
    {
        [SerializeField] private string spriteKey = "";
        [SerializeField] private string tintColorKey = ""; // optional

        public string SpriteKey => spriteKey;
        public string TintColorKey => tintColorKey;
    }

    /// <summary>Visual definition for a Toggle.</summary>
    [Serializable]
    public sealed class ToggleStyleParameters : StyleModuleParameters
    {
        [Header("Sprites")]
        [SerializeField] private string backgroundSpriteKey = "";
        [SerializeField] private string checkmarkSpriteKey = "";

        [Header("Colours")]
        [SerializeField] private string onColorKey = "Primary";
        [SerializeField] private string offColorKey = "Secondary";
        [SerializeField] private string disabledColorKey = "Disabled";

        public string BackgroundSpriteKey => backgroundSpriteKey;
        public string CheckmarkSpriteKey => checkmarkSpriteKey;
        public string OnColorKey => onColorKey;
        public string OffColorKey => offColorKey;
        public string DisabledColorKey => disabledColorKey;
    }
}
