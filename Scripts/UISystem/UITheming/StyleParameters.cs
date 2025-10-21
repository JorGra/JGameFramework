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
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] private string colorKey = "Text";
        [ThemeKey(ThemeKeyKind.Font)]
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
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string spriteKey = "";
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] private string tintColorKey = ""; // optional

        public string SpriteKey => spriteKey;
        public string TintColorKey => tintColorKey;
    }

    /// <summary>Visual definition for a Toggle.</summary>
    [Serializable]
    public sealed class ToggleStyleParameters : StyleModuleParameters
    {
        // --- Static visuals ----------------------------------------------------
        [Header("Background")]
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] string backgroundSpriteKey = "";
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] string backgroundColorKey = "";

        [Header("Fill / Check-mark")]
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] string checkmarkSpriteKey = "";
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] string checkmarkColorKey = "";

        // --- Interaction -------------------------------
        [Header("Interaction")]
        [SerializeField]
        Selectable.Transition transition =
            Selectable.Transition.ColorTint;

        // Used when transition == ColorTint
        [SerializeField] ColorTintBlock colorTint = new();

        // Used when transition == SpriteSwap
        [SerializeField] SpriteSwapBlock spriteSwap = new();

        // --- Public accessors --------------------------------------------------
        public string BackgroundSpriteKey => backgroundSpriteKey;
        public string BackgroundColorKey => backgroundColorKey;
        public string CheckmarkSpriteKey => checkmarkSpriteKey;
        public string CheckmarkColorKey => checkmarkColorKey;

        public Selectable.Transition Transition => transition;
        public ColorTintBlock ColorTint => colorTint;
        public SpriteSwapBlock SpriteSwap => spriteSwap;

        // --- Helper structs ----------------------------------------------------
        [Serializable]
        public struct ColorTintBlock
        {
            [ThemeKey(ThemeKeyKind.Color)] public string normalColorKey;
            [ThemeKey(ThemeKeyKind.Color)] public string highlightedColorKey;
            [ThemeKey(ThemeKeyKind.Color)] public string pressedColorKey;
            [ThemeKey(ThemeKeyKind.Color)] public string selectedColorKey;
            [ThemeKey(ThemeKeyKind.Color)] public string disabledColorKey;
            public float colorMultiplier;   // default 1
            public float fadeDuration;      // default 0.1
        }

        [Serializable]
        public struct SpriteSwapBlock
        {
            [ThemeKey(ThemeKeyKind.Sprite)] public string highlightedSpriteKey;
            [ThemeKey(ThemeKeyKind.Sprite)] public string pressedSpriteKey;
            [ThemeKey(ThemeKeyKind.Sprite)] public string selectedSpriteKey;
            [ThemeKey(ThemeKeyKind.Sprite)] public string disabledSpriteKey;
        }
    }

    /// <summary>Animated button definition with per-state visuals.</summary>
    [Serializable]
    public sealed class ButtonStyleParameters : StyleModuleParameters
    {
        [Header("Label Styling")]
        [Tooltip("Style key of a TextStyleParameters entry applied to the button label when states change.")]
        [ThemeKey(typeof(TextStyleParameters))]
        [SerializeField] private string sharedLabelStyleKey = "button";
        [Tooltip("Toggle to reapply the shared label style whenever the button swaps state.")]
        [SerializeField] private bool applySharedLabelStyle = true;

        [Header("Icon Styling")]
        [Tooltip("Include an icon graphic that follows the button state.")]
        [SerializeField] private bool includeIcon;
        [Tooltip("Style key applied to the icon when Include Icon is enabled.")]
        [ThemeKey(typeof(TextStyleParameters))]
        [SerializeField] private string iconStyleKey = "icon";

        [Header("Animation Settings")]
        [Tooltip("Global easing configuration used when blending between interaction states.")]
        [SerializeField] private AnimationSettings animation = AnimationSettings.Default();

        [Header("Defaults")]
        [Tooltip("Values shared by all button states when a property is not overridden.")]
        [SerializeField] private ButtonDefaults defaults = new ButtonDefaults();

        [Header("States")]
        [Tooltip("Visuals applied when the button is idle or returning from other states.")]
        [SerializeField] private ButtonVisualState normal = ButtonVisualState.Create("normal");
        [Tooltip("Visuals triggered when the pointer hovers over the button.")]
        [SerializeField] private ButtonVisualState highlighted = ButtonVisualState.Create("highlighted");
        [Tooltip("Visuals triggered while the button is pressed.")]
        [SerializeField] private ButtonVisualState pressed = ButtonVisualState.Create("pressed");
        [Tooltip("Visuals used when the button has keyboard or gamepad focus.")]
        [SerializeField] private ButtonVisualState selected = ButtonVisualState.Create("selected");
        [Tooltip("Visuals shown when the button is disabled.")]
        [SerializeField] private ButtonVisualState disabled = ButtonVisualState.Create("disabled");

        public string SharedLabelStyleKey => sharedLabelStyleKey;
        public bool ApplySharedLabelStyle => applySharedLabelStyle;
        public bool IncludeIcon => includeIcon;
        public string IconStyleKey => iconStyleKey;
        public AnimationSettings Animation => animation;
        public ButtonDefaults Defaults => defaults;
        public ButtonVisualState Normal => normal;
        public ButtonVisualState Highlighted => highlighted;
        public ButtonVisualState Pressed => pressed;
        public ButtonVisualState Selected => selected;
        public ButtonVisualState Disabled => disabled;

        [Serializable]
        public sealed class AnimationSettings
        {
            [Tooltip("Seconds spent tweening between two states (0 for instant swap).")]
            [SerializeField] private float duration = 0.12f;
            [Tooltip("Curve sampled to interpolate colours, scale and size.")]
            [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            [Tooltip("Use unscaled time for the transition (ignores Time.timeScale).")]
            [SerializeField] private bool useUnscaledTime;

            public float Duration => Mathf.Max(0f, duration);
            public AnimationCurve Easing => easing ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            public bool UseUnscaledTime => useUnscaledTime;

            public static AnimationSettings Default() => new AnimationSettings();
        }

        [Serializable]
        public sealed class ButtonDefaults
        {
            [TogglePropertyGroup("Background Defaults")]
            [SerializeField] private bool applyBackground;
            [ThemeKey(ThemeKeyKind.Color)]
            [ToggleGroupMember(nameof(applyBackground))]
            [SerializeField] private string backgroundColorKey = string.Empty;
            [ThemeKey(ThemeKeyKind.Sprite)]
            [ToggleGroupMember(nameof(applyBackground))]
            [SerializeField] private string backgroundSpriteKey = string.Empty;
            [ToggleGroupMember(nameof(applyBackground))]
            [SerializeField] private bool useBackgroundImageType;
            [ToggleGroupMember(nameof(applyBackground))]
            [SerializeField] private Image.Type backgroundImageType = Image.Type.Sliced;

            [TogglePropertyGroup("Label Defaults")]
            [SerializeField] private bool applyLabel;
            [ThemeKey(ThemeKeyKind.Color)]
            [ToggleGroupMember(nameof(applyLabel))]
            [SerializeField] private string labelColorKey = string.Empty;
            [ToggleGroupMember(nameof(applyLabel))]
            [SerializeField] private float labelAlpha = 1f;

            [TogglePropertyGroup("Icon Defaults")]
            [SerializeField] private bool applyIcon;
            [ThemeKey(ThemeKeyKind.Color)]
            [ToggleGroupMember(nameof(applyIcon))]
            [SerializeField] private string iconColorKey = string.Empty;
            [ToggleGroupMember(nameof(applyIcon))]
            [SerializeField] private float iconAlpha = 1f;

            [TogglePropertyGroup("Scale Defaults")]
            [SerializeField] private bool applyScale;
            [ToggleGroupMember(nameof(applyScale))]
            [SerializeField] private Vector3 targetScale = Vector3.one;

            [TogglePropertyGroup("Size Defaults")]
            [SerializeField] private bool applySizeDelta;
            [ToggleGroupMember(nameof(applySizeDelta))]
            [SerializeField] private Vector2 targetSizeDelta = Vector2.zero;

            public bool HasBackgroundColor => applyBackground && !string.IsNullOrEmpty(backgroundColorKey);
            public string BackgroundColorKey => backgroundColorKey;
            public bool HasBackgroundSprite => applyBackground && !string.IsNullOrEmpty(backgroundSpriteKey);
            public string BackgroundSpriteKey => backgroundSpriteKey;
            public bool HasBackgroundImageType => applyBackground && useBackgroundImageType;
            public Image.Type BackgroundImageType => backgroundImageType;
            public bool HasLabelColor => applyLabel && !string.IsNullOrEmpty(labelColorKey);
            public string LabelColorKey => labelColorKey;
            public bool HasLabelAlpha => applyLabel;
            public float LabelAlpha => Mathf.Max(0f, labelAlpha);
            public bool HasIconColor => applyIcon && !string.IsNullOrEmpty(iconColorKey);
            public string IconColorKey => iconColorKey;
            public bool HasIconAlpha => applyIcon;
            public float IconAlpha => Mathf.Max(0f, iconAlpha);
            public bool HasScale => applyScale;
            public Vector3 Scale => targetScale;
            public bool HasSizeDelta => applySizeDelta;
            public Vector2 SizeDelta => targetSizeDelta;
        }

        [Serializable]
        public sealed class ButtonVisualState
        {
            [Tooltip("Editor-facing label to help identify this state.")]
            [SerializeField] private string displayName;
            [TogglePropertyGroup("Background")]
            [SerializeField] private bool overrideBackground;
            [Tooltip("Theme colour applied to the button background. Leave empty to inherit from defaults/Normal.")]
            [ThemeKey(ThemeKeyKind.Color)]
            [ToggleGroupMember(nameof(overrideBackground))]
            [SerializeField] private string backgroundColorKey = string.Empty;
            [Tooltip("Theme sprite displayed on the button background. Leave empty to inherit from defaults/Normal.")]
            [ThemeKey(ThemeKeyKind.Sprite)]
            [ToggleGroupMember(nameof(overrideBackground))]
            [SerializeField] private string backgroundSpriteKey = string.Empty;
            [TogglePropertyGroup("Label")]
            [SerializeField] private bool overrideLabel;
            [Tooltip("Theme colour applied to the label for this state. Leave empty to inherit.")]
            [ThemeKey(ThemeKeyKind.Color)]
            [ToggleGroupMember(nameof(overrideLabel))]
            [SerializeField] private string labelColorKey = string.Empty;
            [Tooltip("Alpha multiplier applied to the final label colour in this state.")]
            [ToggleGroupMember(nameof(overrideLabel))]
            [SerializeField] private float labelAlpha = 1f;
            [TogglePropertyGroup("Icon")]
            [SerializeField] private bool overrideIcon;
            [Tooltip("Theme colour applied to the icon for this state. Leave empty to inherit label/icon tint.")]
            [ThemeKey(ThemeKeyKind.Color)]
            [ToggleGroupMember(nameof(overrideIcon))]
            [SerializeField] private string iconColorKey = string.Empty;
            [Tooltip("Alpha multiplier applied to the icon colour in this state.")]
            [ToggleGroupMember(nameof(overrideIcon))]
            [SerializeField] private float iconAlpha = 1f;
            [Tooltip("Animate the target RectTransform scale when entering this state.")]
            [TogglePropertyGroup("Animate Scale")]
            [SerializeField] private bool animateScale;
            [Tooltip("Scale assigned to the RectTransform if Animate Scale is enabled.")]
            [ToggleGroupMember(nameof(animateScale))]
            [SerializeField] private Vector3 targetScale = Vector3.one;
            [Tooltip("Animate the RectTransform sizeDelta when entering this state.")]
            [TogglePropertyGroup("Animate Size")]
            [SerializeField] private bool animateSizeDelta;
            [Tooltip("sizeDelta assigned to the RectTransform if Animate Size Delta is enabled.")]
            [ToggleGroupMember(nameof(animateSizeDelta))]
            [SerializeField] private Vector2 targetSizeDelta = Vector2.zero;
            [Tooltip("Override the shared label style with a dedicated TextStyleParameters entry.")]
            [TogglePropertyGroup("Label Style Override")]
            [SerializeField] private bool useLabelStyleOverride;
            [Tooltip("Style key of the TextStyleParameters override used when the override toggle is set.")]
            [ThemeKey(typeof(TextStyleParameters))]
            [ToggleGroupMember(nameof(useLabelStyleOverride))]
            [SerializeField] private string labelStyleOverrideKey = string.Empty;

            public string DisplayName => displayName;
            public bool OverrideBackground => overrideBackground;
            public string BackgroundColorKey => backgroundColorKey;
            public string BackgroundSpriteKey => backgroundSpriteKey;
            public bool HasBackgroundColor => overrideBackground && !string.IsNullOrEmpty(backgroundColorKey);
            public bool HasBackgroundSprite => overrideBackground && !string.IsNullOrEmpty(backgroundSpriteKey);
            public bool OverrideLabel => overrideLabel;
            public string LabelColorKey => labelColorKey;
            public float LabelAlpha => Mathf.Clamp01(labelAlpha);
            public bool HasLabelColor => overrideLabel && !string.IsNullOrEmpty(labelColorKey);
            public bool HasLabelAlpha => overrideLabel;
            public bool OverrideIcon => overrideIcon;
            public string IconColorKey => iconColorKey;
            public float IconAlpha => Mathf.Clamp01(iconAlpha);
            public bool HasIconColor => overrideIcon && !string.IsNullOrEmpty(iconColorKey);
            public bool HasIconAlpha => overrideIcon;
            public bool AnimateScale => animateScale;
            public Vector3 TargetScale => targetScale;
            public bool AnimateSizeDelta => animateSizeDelta;
            public Vector2 TargetSizeDelta => targetSizeDelta;
            public bool UseLabelStyleOverride => useLabelStyleOverride && !string.IsNullOrEmpty(labelStyleOverrideKey);
            public string LabelStyleOverrideKey => labelStyleOverrideKey;

            public static ButtonVisualState Create(string name) => new ButtonVisualState
            {
                displayName = name,
                targetScale = Vector3.one,
                labelAlpha = 1f,
                iconAlpha = 1f
            };
        }
    }

    /// <summary>Visual definition for a <see cref="UnityEngine.UI.ScrollRect"/> with both scrollbars.</summary>
    [System.Serializable]
    public sealed class ScrollRectStyleParameters : StyleModuleParameters
    {
        // --- Panel (the Image on the ScrollRect root) ----------------------------
        [Header("Panel")]
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string panelSpriteKey = "";
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] private string panelColorKey = "Background";

        // --- Horizontal scrollbar -----------------------------------------------
        [Header("Horizontal Scrollbar")]
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string hBackgroundSpriteKey = "";
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string hHandleSpriteKey = "";

        // --- Vertical scrollbar -------------------------------------------------
        [Header("Vertical Scrollbar")]
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string vBackgroundSpriteKey = "";
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string vHandleSpriteKey = "";

        // --- Interaction mode --------------------------------------------------
        [Header("Scrollbar Interaction")]
        [SerializeField]
        private Selectable.Transition scrollbarTransition = Selectable.Transition.ColorTint;

        // --- Color Tint Settings (used when transition = ColorTint) ------------
        [Header("Color Tint Settings")]
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] private string normalColorKey = "";
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] private string highlightedColorKey = "";
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] private string pressedColorKey = "";
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] private string selectedColorKey = "";
        [ThemeKey(ThemeKeyKind.Color)]
        [SerializeField] private string disabledColorKey = "";
        [SerializeField] private float colorMultiplier = 1f;
        [SerializeField] private float fadeDuration = 0.1f;

        // --- Sprite Swap Settings (used when transition = SpriteSwap) ----------
        [Header("Sprite Swap Settings")]
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string highlightedHandleSpriteKey = "";
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string pressedHandleSpriteKey = "";
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string selectedHandleSpriteKey = "";
        [ThemeKey(ThemeKeyKind.Sprite)]
        [SerializeField] private string disabledHandleSpriteKey = "";

        // --- public accessors ---------------------------------------------------
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
