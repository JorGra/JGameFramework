// TextStyleParameters.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    /// <summary>Style definition for a <see cref="UnityEngine.UI.Button"/>.</summary>
    [Serializable]
    public sealed class ButtonStyleParameters : StyleModuleParameters
    {
        // ─── Visual ─────────────────────────────────────────────────────────────
        [Header("Button Visual")]
        [SerializeField] private string backgroundSpriteKey = "";
        [SerializeField] private string backgroundColorKey = "Primary";

        // ─── Interaction ────────────────────────────────────────────────────────
        [Header("Interaction")]
        [SerializeField]
        private Selectable.Transition transition =
            Selectable.Transition.ColorTint;

        // These fields are used only when the mode is SpriteSwap
        [SerializeField] private SpriteSwapBlock spriteSwap = new();

        // ─── Public accessors ───────────────────────────────────────────────────
        public string BackgroundSpriteKey => backgroundSpriteKey;
        public string BackgroundColorKey => backgroundColorKey;
        public Selectable.Transition Transition => transition;
        public SpriteSwapBlock SpriteSwap => spriteSwap;

        // ─── Helper struct for swap mode ────────────────────────────────────────
        [Serializable]
        public struct SpriteSwapBlock
        {
            public string highlightedSpriteKey;   // hover
            public string pressedSpriteKey;       // pointer down
            public string selectedSpriteKey;      // keyboard focus
            public string disabledSpriteKey;
        }
    }

    /// <summary>Visual definition for a <see cref="UnityEngine.UI.ScrollRect"/> with both scrollbars.</summary>
    [System.Serializable]
    public sealed class ScrollRectStyleParameters : StyleModuleParameters
    {
        // ─── Panel (the Image on the ScrollRect root) ────────────────────────────
        [Header("Panel")]
        [SerializeField] private string panelSpriteKey = "";
        [SerializeField] private string panelColorKey = "Background";

        // ─── Horizontal scrollbar ───────────────────────────────────────────────
        [Header("Horizontal Scrollbar")]
        [SerializeField] private string hBackgroundSpriteKey = "";
        [SerializeField] private string hHandleSpriteKey = "";

        // ─── Vertical scrollbar ─────────────────────────────────────────────────
        [Header("Vertical Scrollbar")]
        [SerializeField] private string vBackgroundSpriteKey = "";
        [SerializeField] private string vHandleSpriteKey = "";

        // ─── Interaction mode ──────────────────────────────────────────────────
        [Header("Scrollbar Interaction")]
        [SerializeField]
        private Selectable.Transition scrollbarTransition = Selectable.Transition.ColorTint;

        // ─── Color Tint Settings (used when transition = ColorTint) ────────────
        [Header("Color Tint Settings")]
        [SerializeField] private string normalColorKey = "";
        [SerializeField] private string highlightedColorKey = "";
        [SerializeField] private string pressedColorKey = "";
        [SerializeField] private string selectedColorKey = "";
        [SerializeField] private string disabledColorKey = "";
        [SerializeField] private float colorMultiplier = 1f;
        [SerializeField] private float fadeDuration = 0.1f;

        // ─── Sprite Swap Settings (used when transition = SpriteSwap) ──────────
        [Header("Sprite Swap Settings")]
        [SerializeField] private string highlightedHandleSpriteKey = "";
        [SerializeField] private string pressedHandleSpriteKey = "";
        [SerializeField] private string selectedHandleSpriteKey = "";
        [SerializeField] private string disabledHandleSpriteKey = "";

        // ─── public accessors ───────────────────────────────────────────────────
        public string PanelSpriteKey => panelSpriteKey;
        public string PanelColorKey => panelColorKey;
        public string HBackgroundSpriteKey => hBackgroundSpriteKey;
        public string HHandleSpriteKey => hHandleSpriteKey;
        public string VBackgroundSpriteKey => vBackgroundSpriteKey;
        public string VHandleSpriteKey => vHandleSpriteKey;
        public Selectable.Transition ScrollbarTransition => scrollbarTransition;

        // Color tint accessors
        public string NormalColorKey => normalColorKey;
        public string HighlightedColorKey => highlightedColorKey;
        public string PressedColorKey => pressedColorKey;
        public string SelectedColorKey => selectedColorKey;
        public string DisabledColorKey => disabledColorKey;
        public float ColorMultiplier => colorMultiplier;
        public float FadeDuration => fadeDuration;

        // Sprite swap accessors
        public string HighlightedHandleSpriteKey => highlightedHandleSpriteKey;
        public string PressedHandleSpriteKey => pressedHandleSpriteKey;
        public string SelectedHandleSpriteKey => selectedHandleSpriteKey;
        public string DisabledHandleSpriteKey => disabledHandleSpriteKey;
    }
}
