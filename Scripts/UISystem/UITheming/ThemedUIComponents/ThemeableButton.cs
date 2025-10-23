// ───────────────────────────────────────────────────────── ThemeableButton.cs
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Theming
{
    /// <summary>
    /// Animated themed button that drives image, text and layout transitions using <see cref="ButtonStyleParameters"/>.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ThemeableButton : MonoBehaviour, IThemeable,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        enum VisualState
        {
            Normal,
            Highlighted,
            Pressed,
            Selected,
            Disabled
        }

        struct ResolvedButtonState
        {
            public string Name;
            public Color? BackgroundColor;
            public Sprite BackgroundSprite;
            public Image.Type? BackgroundImageType;
            public float? BackgroundPixelPerUnit;
            public Color? LabelColor;
            public float LabelAlpha;
            public Vector3? Scale;
            public Vector2? SizeDelta;
            public string LabelStyleOverride;
            public Color? IconColor;
            public float IconAlpha;
        }

        [ThemeKey(typeof(ButtonStyleParameters))]
        [SerializeField] private string styleKey = "Default";
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Graphic icon;
        [SerializeField] private RectTransform animatedTransform;
        [SerializeField] bool customPixelPerUnit = false;
        RectTransform baselineSource;

        Button button;
        ThemeAsset currentTheme;
        ButtonStyleParameters style;
        ResolvedButtonState defaults;
        ResolvedButtonState normal;
        ResolvedButtonState highlighted;
        ResolvedButtonState pressed;
        ResolvedButtonState selected;
        ResolvedButtonState disabled;
        VisualState currentVisualState = VisualState.Normal;
        Coroutine animationRoutine;
        Vector3 initialScale;
        Vector2 initialSizeDelta;
        bool baselineCaptured;
        bool lastInteractable;
        bool pointerInside;
        bool pointerDown;
        bool hasSelection;
        bool DrivesBackgroundColor => style == null || style.SelectableTransition != Selectable.Transition.ColorTint;
        bool DrivesBackgroundSprite => style == null || style.SelectableTransition != Selectable.Transition.SpriteSwap;

        void Awake() => CacheReferences();

        void OnEnable()
        {
            this.SubscribeEvent<ThemeChangedEvent>(e => ApplyTheme(e.Theme));
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
            lastInteractable = IsInteractable;
            ApplyVisualState(IsInteractable ? VisualState.Normal : VisualState.Disabled, true);
        }

        void OnDisable()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
        }

        void LateUpdate()
        {
            bool interactable = IsInteractable;
            if (interactable != lastInteractable)
            {
                lastInteractable = interactable;
                ApplyVisualState(interactable ? VisualState.Normal : VisualState.Disabled);
            }
        }

        public void ApplyTheme(ThemeAsset theme)
        {
            CacheReferences();

            currentTheme = theme;
            if (theme == null || !theme.TryGetStyle(styleKey, out ButtonStyleParameters parameters))
            {
                style = null;
                return;
            }

            style = parameters;
            ResolveStates();
            ConfigureSelectableTransition();
            ApplyLabelStyleForState(normal);
            ApplyIconStyle();
            if (style.IncludeIcon && icon == null)
            {
                Debug.LogWarning($"ThemeableButton '{name}' expects an icon reference for style '{styleKey}', but none is assigned.", this);
            }
            ApplyStateInstant(normal, false);
            if (animatedTransform)
            {
                initialScale = animatedTransform.localScale;
                initialSizeDelta = animatedTransform.sizeDelta;
            }
            currentVisualState = VisualState.Normal;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            if (!IsInteractable) return;
            if (!pointerDown)
            {
                ApplyVisualState(VisualState.Highlighted);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            if (!IsInteractable) return;
            if (pointerDown)
            {
                ApplyVisualState(hasSelection ? VisualState.Selected : VisualState.Normal);
            }
            else
            {
                ApplyVisualState(hasSelection ? VisualState.Selected : VisualState.Normal);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable) return;
            pointerDown = true;
            ApplyVisualState(VisualState.Pressed);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable) return;
            pointerDown = false;
            ApplyVisualState(pointerInside ? VisualState.Highlighted : (hasSelection ? VisualState.Selected : VisualState.Normal));
        }

        public void OnSelect(BaseEventData eventData)
        {
            hasSelection = true;
            if (!IsInteractable) return;
            ApplyVisualState(VisualState.Selected);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hasSelection = false;
            if (!IsInteractable) return;
            ApplyVisualState(pointerInside ? VisualState.Highlighted : VisualState.Normal);
        }

        bool IsInteractable => button != null && button.IsActive() && button.interactable;

        void CacheReferences()
        {
            button = button ? button : GetComponent<Button>();

            if (!background)
            {
                background = button != null
                    ? button.targetGraphic as Image ?? GetComponent<Image>()
                    : GetComponent<Image>();
            }

            if (!animatedTransform)
            {
                animatedTransform = transform as RectTransform;
            }

            if (animatedTransform != baselineSource)
            {
                baselineSource = animatedTransform;
                baselineCaptured = false;
            }

            if (!label)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }

            if (!icon)
            {
                var graphics = GetComponentsInChildren<Graphic>(true);
                for (int i = 0; i < graphics.Length; i++)
                {
                    var candidate = graphics[i];
                    if (candidate == null) continue;
                    if (candidate == background) continue;
                    if (label != null && candidate == label) continue;
                    if (button != null && candidate == button.targetGraphic) continue;
                    icon = candidate;
                    break;
                }
            }

            if (animatedTransform != null && !baselineCaptured)
            {
                initialScale = animatedTransform.localScale;
                initialSizeDelta = animatedTransform.sizeDelta;
                baselineCaptured = true;
            }

            if (button != null)
            {
            }
        }

        void ResolveStates()
        {
            defaults = ResolveDefaults(style.Defaults);
            normal = Resolve(style.Normal, null);
            highlighted = Resolve(style.Highlighted, style.Normal);
            pressed = Resolve(style.Pressed, style.Normal);
            selected = Resolve(style.Selected, style.Normal);
            disabled = Resolve(style.Disabled, style.Normal);
        }

        void ConfigureSelectableTransition()
        {
            if (!button || style == null)
            {
                return;
            }

            if (!style.UsesUnitySelectableTransition)
            {
                button.transition = Selectable.Transition.None;
                button.spriteState = default;
                return;
            }

            if (!button.targetGraphic && background)
            {
                button.targetGraphic = background;
            }

            button.transition = style.SelectableTransition;

            if (style.SelectableTransition == Selectable.Transition.ColorTint)
            {
                var colors = button.colors;

                colors.normalColor = ResolveTransitionColor(normal, colors.normalColor);
                colors.highlightedColor = ResolveTransitionColor(highlighted, colors.highlightedColor);
                colors.pressedColor = ResolveTransitionColor(pressed, colors.pressedColor);
                colors.selectedColor = ResolveTransitionColor(selected, colors.selectedColor);
                colors.disabledColor = ResolveTransitionColor(disabled, colors.disabledColor);

                button.colors = colors;

                if (button.targetGraphic)
                {
                    button.targetGraphic.color = colors.normalColor;
                }
            }
            else if (style.SelectableTransition == Selectable.Transition.SpriteSwap)
            {
                var spriteState = button.spriteState;
                spriteState.highlightedSprite = ResolveTransitionSprite(highlighted, spriteState.highlightedSprite);
                spriteState.pressedSprite = ResolveTransitionSprite(pressed, spriteState.pressedSprite);
                spriteState.selectedSprite = ResolveTransitionSprite(selected, spriteState.selectedSprite);
                spriteState.disabledSprite = ResolveTransitionSprite(disabled, spriteState.disabledSprite);
                button.spriteState = spriteState;
            }

            ApplySelectableBaseline(style.SelectableTransition);
        }

        Color ResolveTransitionColor(ResolvedButtonState state, Color fallback)
        {
            var color = ResolveBackgroundColor(state, true);
            if (color.HasValue)
            {
                return color.Value;
            }

            return fallback;
        }

        Sprite ResolveTransitionSprite(ResolvedButtonState state, Sprite fallback)
        {
            var sprite = ResolveSprite(state, true, out _);
            return sprite ? sprite : fallback;
        }

        void ApplySelectableBaseline(Selectable.Transition transition)
        {
            if (transition == Selectable.Transition.None || !background)
            {
                return;
            }

            if (normal.BackgroundSprite)
            {
                background.sprite = normal.BackgroundSprite;
            }

            if (normal.BackgroundImageType.HasValue)
            {
                background.type = normal.BackgroundImageType.Value;
            }

            if (normal.BackgroundPixelPerUnit.HasValue && !customPixelPerUnit)
            {
                background.pixelsPerUnitMultiplier = Mathf.Max(0.001f, normal.BackgroundPixelPerUnit.Value);
            }

            if (transition == Selectable.Transition.ColorTint)
            {
                var baseColor = ResolveTransitionColor(normal, background.color);
                background.color = baseColor;
                if (button && button.targetGraphic == background)
                {
                    button.targetGraphic.color = baseColor;
                }
            }
        }

        ResolvedButtonState ResolveDefaults(ButtonStyleParameters.ButtonDefaults defaultsConfig)
        {
            var resolved = new ResolvedButtonState
            {
                Name = "defaults",
                LabelAlpha = 1f,
                IconAlpha = 1f
            };

            if (defaultsConfig == null || currentTheme == null)
            {
                return resolved;
            }

            if (defaultsConfig.HasBackgroundColor)
            {
                resolved.BackgroundColor = currentTheme.GetColor(defaultsConfig.BackgroundColorKey);
            }

            if (defaultsConfig.HasBackgroundSprite)
            {
                var sprite = currentTheme.GetSprite(defaultsConfig.BackgroundSpriteKey);
                resolved.BackgroundSprite = sprite;
                resolved.BackgroundPixelPerUnit = currentTheme.GetSpritePixelPerUnit(defaultsConfig.BackgroundSpriteKey);
                if (defaultsConfig.HasBackgroundImageType)
                {
                    resolved.BackgroundImageType = defaultsConfig.BackgroundImageType;
                }
            }

            if (defaultsConfig.HasLabelColor)
            {
                resolved.LabelColor = currentTheme.GetColor(defaultsConfig.LabelColorKey);
            }

            if (defaultsConfig.HasLabelAlpha)
            {
                resolved.LabelAlpha = Mathf.Max(0f, defaultsConfig.LabelAlpha);
            }

            if (style != null && style.IncludeIcon)
            {
                if (defaultsConfig.HasIconColor)
                {
                    resolved.IconColor = currentTheme.GetColor(defaultsConfig.IconColorKey);
                }

                if (defaultsConfig.HasIconAlpha)
                {
                    resolved.IconAlpha = Mathf.Max(0f, defaultsConfig.IconAlpha);
                }
            }

            if (defaultsConfig.HasScale)
            {
                resolved.Scale = defaultsConfig.Scale;
            }

            if (defaultsConfig.HasSizeDelta)
            {
                resolved.SizeDelta = defaultsConfig.SizeDelta;
            }

            return resolved;
        }

        ResolvedButtonState Resolve(ButtonStyleParameters.ButtonVisualState state, ButtonStyleParameters.ButtonVisualState fallback)
        {
            if (state == null)
            {
                return defaults;
            }

            float defaultLabelAlpha = defaults.LabelAlpha > 0f ? defaults.LabelAlpha : 1f;
            float defaultIconAlpha = style != null && style.IncludeIcon
                ? (defaults.IconAlpha > 0f ? defaults.IconAlpha : 1f)
                : 1f;

            var resolved = new ResolvedButtonState
            {
                Name = state.DisplayName,
                LabelAlpha = ResolveFloat(state, fallback, s => s.HasLabelAlpha, s => s.LabelAlpha, defaultLabelAlpha),
                IconAlpha = style != null && style.IncludeIcon
                    ? ResolveFloat(state, fallback, s => s.HasIconAlpha, s => s.IconAlpha, defaultIconAlpha)
                    : 1f
            };

            resolved.LabelStyleOverride = ResolveLabelStyleOverride(state, fallback);

            resolved.BackgroundColor = ResolveThemeColor(state, fallback, s => s.HasBackgroundColor, s => s.BackgroundColorKey);
            if (!resolved.BackgroundColor.HasValue && defaults.BackgroundColor.HasValue)
            {
                resolved.BackgroundColor = defaults.BackgroundColor;
            }

            var sprite = ResolveThemeSprite(state, fallback, s => s.HasBackgroundSprite, s => s.BackgroundSpriteKey, out var imageType, out var pixelPerUnit);
            if (sprite != null)
            {
                resolved.BackgroundSprite = sprite;
                resolved.BackgroundImageType = imageType;
                resolved.BackgroundPixelPerUnit = pixelPerUnit;
            }
            else if (defaults.BackgroundSprite != null)
            {
                resolved.BackgroundSprite = defaults.BackgroundSprite;
                resolved.BackgroundImageType = defaults.BackgroundImageType;
                resolved.BackgroundPixelPerUnit = defaults.BackgroundPixelPerUnit;
            }
            else if (defaults.BackgroundImageType.HasValue)
            {
                resolved.BackgroundImageType = defaults.BackgroundImageType;
            }

            resolved.LabelColor = ResolveThemeColor(state, fallback, s => s.HasLabelColor, s => s.LabelColorKey);
            if (!resolved.LabelColor.HasValue && defaults.LabelColor.HasValue)
            {
                resolved.LabelColor = defaults.LabelColor;
            }

            if (style != null && style.IncludeIcon)
            {
                resolved.IconColor = ResolveThemeColor(state, fallback, s => s.HasIconColor, s => s.IconColorKey);
                if (!resolved.IconColor.HasValue && defaults.IconColor.HasValue)
                {
                    resolved.IconColor = defaults.IconColor;
                }
            }

            bool inheritsScale = state.AnimateScale || (fallback != null && fallback.AnimateScale);
            if (state.AnimateScale)
            {
                resolved.Scale = state.TargetScale;
            }
            else if (fallback != null && fallback.AnimateScale)
            {
                resolved.Scale = fallback.TargetScale;
            }
            else if (!resolved.Scale.HasValue && inheritsScale && defaults.Scale.HasValue)
            {
                resolved.Scale = defaults.Scale;
            }

            bool inheritsSize = state.AnimateSizeDelta || (fallback != null && fallback.AnimateSizeDelta);
            if (state.AnimateSizeDelta)
            {
                resolved.SizeDelta = state.TargetSizeDelta;
            }
            else if (fallback != null && fallback.AnimateSizeDelta)
            {
                resolved.SizeDelta = fallback.TargetSizeDelta;
            }
            else if (!resolved.SizeDelta.HasValue && inheritsSize && defaults.SizeDelta.HasValue)
            {
                resolved.SizeDelta = defaults.SizeDelta;
            }

            return resolved;
        }

        string ResolveLabelStyleOverride(
            ButtonStyleParameters.ButtonVisualState primary,
            ButtonStyleParameters.ButtonVisualState fallback)
        {
            if (primary != null && primary.UseLabelStyleOverride)
            {
                return primary.LabelStyleOverrideKey;
            }

            if (fallback != null && fallback.UseLabelStyleOverride)
            {
                return fallback.LabelStyleOverrideKey;
            }

            return null;
        }

        float ResolveFloat(
            ButtonStyleParameters.ButtonVisualState primary,
            ButtonStyleParameters.ButtonVisualState fallback,
            Func<ButtonStyleParameters.ButtonVisualState, bool> hasValue,
            Func<ButtonStyleParameters.ButtonVisualState, float> getter,
            float defaultValue)
        {
            if (primary != null && hasValue(primary))
            {
                return getter(primary);
            }

            if (fallback != null && hasValue(fallback))
            {
                return getter(fallback);
            }

            return defaultValue;
        }

        Color? ResolveThemeColor(
            ButtonStyleParameters.ButtonVisualState primary,
            ButtonStyleParameters.ButtonVisualState fallback,
            Func<ButtonStyleParameters.ButtonVisualState, bool> hasValue,
            Func<ButtonStyleParameters.ButtonVisualState, string> keySelector)
        {
            string key = ResolveThemeKey(primary, fallback, hasValue, keySelector);
            if (string.IsNullOrEmpty(key) || currentTheme == null)
            {
                return null;
            }

            return currentTheme.GetColor(key);
        }

        Sprite ResolveThemeSprite(
            ButtonStyleParameters.ButtonVisualState primary,
            ButtonStyleParameters.ButtonVisualState fallback,
            Func<ButtonStyleParameters.ButtonVisualState, bool> hasValue,
            Func<ButtonStyleParameters.ButtonVisualState, string> keySelector,
            out Image.Type? imageType,
            out float? pixelPerUnit)
        {
            string key = ResolveThemeKey(primary, fallback, hasValue, keySelector);
            if (string.IsNullOrEmpty(key) || currentTheme == null)
            {
                imageType = null;
                pixelPerUnit = null;
                return null;
            }

            imageType = currentTheme.GetSpriteType(key);
            pixelPerUnit = currentTheme.GetSpritePixelPerUnit(key);
            return currentTheme.GetSprite(key);
        }

        string ResolveThemeKey(
            ButtonStyleParameters.ButtonVisualState primary,
            ButtonStyleParameters.ButtonVisualState fallback,
            Func<ButtonStyleParameters.ButtonVisualState, bool> hasValue,
            Func<ButtonStyleParameters.ButtonVisualState, string> keySelector)
        {
            if (primary != null && hasValue(primary))
            {
                return keySelector(primary);
            }

            if (fallback != null && hasValue(fallback))
            {
                return keySelector(fallback);
            }

            return null;
        }

        void ApplyVisualState(VisualState targetState, bool immediate = false)
        {
            if (!IsInteractable)
            {
                targetState = VisualState.Disabled;
            }

            if (currentVisualState == targetState && !immediate)
            {
                return;
            }

            currentVisualState = targetState;
            var state = GetResolvedState(targetState);
            bool fallbackToNormal = targetState != VisualState.Normal;
            ApplyLabelStyleForState(state);

            if (style == null)
            {
                ApplyStateInstant(state, fallbackToNormal);
                return;
            }

            var settings = style.Animation;
            if (immediate || settings.Duration <= 0f)
            {
                ApplyStateInstant(state, fallbackToNormal);
                return;
            }

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            animationRoutine = StartCoroutine(AnimateToState(state, settings, fallbackToNormal));
        }

        IEnumerator AnimateToState(ResolvedButtonState target, ButtonStyleParameters.AnimationSettings settings, bool fallbackToNormal)
        {
            float duration = settings.Duration;
            var curve = settings.Easing;
            bool useUnscaled = settings.UseUnscaledTime;

            Color? startBackgroundColor = null;
            Color? targetBackgroundColor = null;
            if (background && DrivesBackgroundColor)
            {
                startBackgroundColor = background.color;
                targetBackgroundColor = ResolveBackgroundColor(target, fallbackToNormal);
            }
            Color? startLabelColor = label ? label.color : null;
            Color? targetLabelColor = null;

            if (label)
            {
                var baseColor = startLabelColor ?? Color.white;
                baseColor = ResolveLabelColor(target, fallbackToNormal, baseColor);
                targetLabelColor = baseColor;
            }

            Color? startIconColor = style != null && style.IncludeIcon && icon ? (Color?)icon.color : null;
            Color? targetIconColor = null;
            if (style != null && style.IncludeIcon && icon)
            {
                var baseIconColor = startIconColor ?? icon.color;
                targetIconColor = ResolveIconColor(target, fallbackToNormal, baseIconColor);
            }

            Vector3? startScale = animatedTransform ? animatedTransform.localScale : (Vector3?)null;
            Vector3? targetScale = animatedTransform ? ResolveScale(target, fallbackToNormal) : (Vector3?)null;

            Vector2? startSize = animatedTransform ? animatedTransform.sizeDelta : (Vector2?)null;
            Vector2? targetSize = animatedTransform ? ResolveSize(target, fallbackToNormal) : (Vector2?)null;

            if (background)
            {
                if (DrivesBackgroundSprite)
                {
                    var sprite = ResolveSprite(target, fallbackToNormal, out var pixelPerUnit);
                    if (sprite)
                    {
                        background.sprite = sprite;
                    }

                    if (pixelPerUnit.HasValue && !customPixelPerUnit)
                    {
                        background.pixelsPerUnitMultiplier = Mathf.Max(0.001f, pixelPerUnit.Value);
                    }

                    var type = ResolveImageType(target, fallbackToNormal);
                    if (type.HasValue)
                    {
                        background.type = type.Value;
                    }
                }
                else if (!customPixelPerUnit && normal.BackgroundPixelPerUnit.HasValue)
                {
                    background.pixelsPerUnitMultiplier = Mathf.Max(0.001f, normal.BackgroundPixelPerUnit.Value);
                }
            }

            float time = 0f;
            while (time < duration)
            {
                float t = duration > 0f ? curve.Evaluate(Mathf.Clamp01(time / duration)) : 1f;

                if (background && DrivesBackgroundColor && startBackgroundColor.HasValue && targetBackgroundColor.HasValue)
                {
                    background.color = Color.LerpUnclamped(startBackgroundColor.Value, targetBackgroundColor.Value, t);
                }

                if (label && startLabelColor.HasValue && targetLabelColor.HasValue)
                {
                    label.color = Color.LerpUnclamped(startLabelColor.Value, targetLabelColor.Value, t);
                }

                if (style != null && style.IncludeIcon && icon && startIconColor.HasValue && targetIconColor.HasValue)
                {
                    icon.color = Color.LerpUnclamped(startIconColor.Value, targetIconColor.Value, t);
                }

                if (animatedTransform && startScale.HasValue && targetScale.HasValue)
                {
                    animatedTransform.localScale = Vector3.LerpUnclamped(startScale.Value, targetScale.Value, t);
                }

                if (animatedTransform && startSize.HasValue && targetSize.HasValue)
                {
                    animatedTransform.sizeDelta = Vector2.LerpUnclamped(startSize.Value, targetSize.Value, t);
                }

                time += useUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            if (background && DrivesBackgroundColor && targetBackgroundColor.HasValue)
            {
                background.color = targetBackgroundColor.Value;
            }

            if (label && targetLabelColor.HasValue)
            {
                label.color = targetLabelColor.Value;
            }

            if (style != null && style.IncludeIcon && icon && targetIconColor.HasValue)
            {
                icon.color = targetIconColor.Value;
            }

            if (animatedTransform && targetScale.HasValue)
            {
                animatedTransform.localScale = targetScale.Value;
            }

            if (animatedTransform && targetSize.HasValue)
            {
                animatedTransform.sizeDelta = targetSize.Value;
            }

            animationRoutine = null;
        }

        void ApplyStateInstant(ResolvedButtonState state, bool fallbackToNormal)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            if (background)
            {
                if (DrivesBackgroundSprite)
                {
                    var sprite = ResolveSprite(state, fallbackToNormal, out var pixelPerUnit);
                    if (sprite)
                    {
                        background.sprite = sprite;
                        if (pixelPerUnit.HasValue && !customPixelPerUnit)
                        {
                            background.pixelsPerUnitMultiplier = Mathf.Max(0.001f, pixelPerUnit.Value);
                        }

                        var type = ResolveImageType(state, fallbackToNormal);
                        if (type.HasValue)
                        {
                            background.type = type.Value;
                        }
                    }
                    else if (pixelPerUnit.HasValue && !customPixelPerUnit)
                    {
                        background.pixelsPerUnitMultiplier = Mathf.Max(0.001f, pixelPerUnit.Value);
                    }
                }

                if (DrivesBackgroundColor)
                {
                    var color = ResolveBackgroundColor(state, fallbackToNormal);
                    if (color.HasValue)
                    {
                        background.color = color.Value;
                    }
                }
            }

            if (label)
            {
                var baseColor = label.color;
                label.color = ResolveLabelColor(state, fallbackToNormal, baseColor);
            }

            if (style != null && style.IncludeIcon && icon)
            {
                var baseIconColor = icon.color;
                icon.color = ResolveIconColor(state, fallbackToNormal, baseIconColor);
            }

            if (animatedTransform)
            {
                animatedTransform.localScale = ResolveScale(state, fallbackToNormal);
                animatedTransform.sizeDelta = ResolveSize(state, fallbackToNormal);
            }
        }

        ResolvedButtonState GetResolvedState(VisualState state) => state switch
        {
            VisualState.Highlighted => highlighted,
            VisualState.Pressed => pressed,
            VisualState.Selected => selected,
            VisualState.Disabled => disabled,
            _ => normal
        };

        void ApplyLabelStyleForState(ResolvedButtonState state)
        {
            if (label == null || currentTheme == null || style == null)
            {
                return;
            }

            string targetKey = state.LabelStyleOverride ?? (style.ApplySharedLabelStyle ? style.SharedLabelStyleKey : null);
            if (string.IsNullOrEmpty(targetKey))
            {
                return;
            }

            if (currentTheme.TryGetStyle(targetKey, out TextStyleParameters textStyle))
            {
                bool usesStateColor = state.LabelColor.HasValue;
                bool usesDefaultColor = !usesStateColor && defaults.LabelColor.HasValue;
                ApplyTextStyle(textStyle, !(usesStateColor || usesDefaultColor));

                if (usesStateColor)
                {
                    label.color = state.LabelColor.Value;
                }
                else if (usesDefaultColor)
                {
                    label.color = defaults.LabelColor.Value;
                }
            }
        }

        void ApplyIconStyle()
        {
            if (icon == null || currentTheme == null || style == null || !style.IncludeIcon)
            {
                return;
            }

            if (!string.IsNullOrEmpty(style.IconStyleKey) &&
                currentTheme.TryGetStyle(style.IconStyleKey, out TextStyleParameters iconStyle))
            {
                icon.color = currentTheme.GetColor(iconStyle.ColorKey);

                if (icon is TMP_Text iconText)
                {
                    iconText.fontStyle = iconStyle.FontStyle;
                    iconText.fontSize = iconStyle.FontSize;
                    var fontAsset = currentTheme.GetFont(iconStyle.FontKey);
                    if (fontAsset != null)
                    {
                        iconText.font = fontAsset;
                    }
                }
            }
        }


        void ApplyTextStyle(TextStyleParameters textStyle, bool applyColor = true)
        {
            if (label == null || currentTheme == null) return;

            label.fontStyle = textStyle.FontStyle;
            if (applyColor)
            {
                label.color = currentTheme.GetColor(textStyle.ColorKey);
            }

            label.fontSize = textStyle.FontSize;
            var fontAsset = currentTheme.GetFont(textStyle.FontKey);
            if (fontAsset != null)
            {
                label.font = fontAsset;
            }
        }

        Color? ResolveBackgroundColor(ResolvedButtonState state, bool fallbackToNormal)
        {
            if (state.BackgroundColor.HasValue)
            {
                return state.BackgroundColor.Value;
            }

            if (fallbackToNormal && normal.BackgroundColor.HasValue)
            {
                return normal.BackgroundColor.Value;
            }

            return background ? background.color : (Color?)null;
        }

        Sprite ResolveSprite(ResolvedButtonState state, bool fallbackToNormal, out float? pixelPerUnit)
        {
            pixelPerUnit = null;

            if (state.BackgroundSprite)
            {
                pixelPerUnit = state.BackgroundPixelPerUnit;
                return state.BackgroundSprite;
            }

            if (fallbackToNormal && normal.BackgroundSprite)
            {
                pixelPerUnit = normal.BackgroundPixelPerUnit;
                return normal.BackgroundSprite;
            }

            if (state.BackgroundPixelPerUnit.HasValue)
            {
                pixelPerUnit = state.BackgroundPixelPerUnit;
            }
            else if (fallbackToNormal && normal.BackgroundPixelPerUnit.HasValue)
            {
                pixelPerUnit = normal.BackgroundPixelPerUnit;
            }

            return background ? background.sprite : null;
        }

        Image.Type? ResolveImageType(ResolvedButtonState state, bool fallbackToNormal)
        {
            if (state.BackgroundImageType.HasValue)
            {
                return state.BackgroundImageType.Value;
            }

            if (fallbackToNormal && normal.BackgroundImageType.HasValue)
            {
                return normal.BackgroundImageType.Value;
            }

            return background ? background.type : (Image.Type?)null;
        }

        Color ResolveLabelColor(ResolvedButtonState state, bool fallbackToNormal, Color currentColor)
        {
            Color color = currentColor;

            if (state.LabelColor.HasValue)
            {
                color = state.LabelColor.Value;
            }
            else if (fallbackToNormal && string.IsNullOrEmpty(state.LabelStyleOverride) && normal.LabelColor.HasValue)
            {
                color = normal.LabelColor.Value;
            }

            color.a = Mathf.Clamp01(color.a * state.LabelAlpha);
            return color;
        }

        Color ResolveIconColor(ResolvedButtonState state, bool fallbackToNormal, Color currentColor)
        {
            if (style == null || !style.IncludeIcon || icon == null)
            {
                return currentColor;
            }

            Color color = currentColor;

            if (state.IconColor.HasValue)
            {
                color = state.IconColor.Value;
            }
            else if (fallbackToNormal && normal.IconColor.HasValue)
            {
                color = normal.IconColor.Value;
            }
            else if (state.LabelColor.HasValue)
            {
                color = state.LabelColor.Value;
            }
            else if (fallbackToNormal && normal.LabelColor.HasValue)
            {
                color = normal.LabelColor.Value;
            }

            float alpha = state.IconAlpha > 0f ? state.IconAlpha : 1f;
            color.a = Mathf.Clamp01(color.a * alpha);
            return color;
        }

        Vector3 ResolveScale(ResolvedButtonState state, bool fallbackToNormal)
        {
            if (state.Scale.HasValue)
            {
                return state.Scale.Value;
            }

            if (fallbackToNormal && normal.Scale.HasValue)
            {
                return normal.Scale.Value;
            }

            return initialScale;
        }

        Vector2 ResolveSize(ResolvedButtonState state, bool fallbackToNormal)
        {
            if (state.SizeDelta.HasValue)
            {
                return state.SizeDelta.Value;
            }

            if (fallbackToNormal && normal.SizeDelta.HasValue)
            {
                return normal.SizeDelta.Value;
            }

            return initialSizeDelta;
        }
    }
}
