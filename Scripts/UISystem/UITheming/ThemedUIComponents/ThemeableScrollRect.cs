using JG.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Theming
{
    /// <summary>
    /// Applies a <see cref="ScrollRectStyleParameters"/> module to a ScrollRect,
    /// styling its panel image plus both scrollbars (background + handle) with
    /// support for both ColorTint and SpriteSwap transition modes.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ThemeableScrollRect : MonoBehaviour, IThemeable
    {
        [SerializeField] private string styleKey = "Default";

        ScrollRect scrollRect;
        Image panelImage;

        Scrollbar hScrollbar;
        Image hBackground, hHandle;
        Sprite originalHBackgroundSprite, originalHHandleSprite;
        ColorBlock originalHColors;

        Scrollbar vScrollbar;
        Image vBackground, vHandle;
        Sprite originalVBackgroundSprite, originalVHandleSprite;
        ColorBlock originalVColors;

        EventBinding<ThemeChangedEvent> binding;

        void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            panelImage = scrollRect.GetComponent<Image>();

            CacheScrollbarReferences(scrollRect.horizontalScrollbar, ref hScrollbar,
                ref hBackground, ref hHandle, ref originalHBackgroundSprite,
                ref originalHHandleSprite, ref originalHColors);

            CacheScrollbarReferences(scrollRect.verticalScrollbar, ref vScrollbar,
                ref vBackground, ref vHandle, ref originalVBackgroundSprite,
                ref originalVHandleSprite, ref originalVColors);
        }

        void CacheScrollbarReferences(Scrollbar scrollbar, ref Scrollbar cachedScrollbar,
            ref Image background, ref Image handle, ref Sprite originalBgSprite,
            ref Sprite originalHandleSprite, ref ColorBlock originalColors)
        {
            if (scrollbar == null) return;

            cachedScrollbar = scrollbar;
            background = scrollbar.GetComponent<Image>();

            if (background)
                originalBgSprite = background.sprite;

            if (scrollbar.handleRect)
            {
                handle = scrollbar.handleRect.GetComponent<Image>();
                if (handle)
                    originalHandleSprite = handle.sprite;
            }

            originalColors = scrollbar.colors;
        }

        void OnEnable()
        {
            binding = new EventBinding<ThemeChangedEvent>(e => ApplyTheme(e.Theme));
            EventBus<ThemeChangedEvent>.Register(binding);
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        void OnDisable() => EventBus<ThemeChangedEvent>.Deregister(binding);

        public void ApplyTheme(ThemeAsset theme)
        {
            if (theme == null || !theme.TryGetStyle(styleKey, out ScrollRectStyleParameters style))
                return;

            ApplyPanelStyle(theme, style);

            if (hScrollbar)
                ApplyScrollbarStyle(hScrollbar, theme, style, hBackground, hHandle,
                    originalHBackgroundSprite, originalHHandleSprite, originalHColors, true);

            if (vScrollbar)
                ApplyScrollbarStyle(vScrollbar, theme, style, vBackground, vHandle,
                    originalVBackgroundSprite, originalVHandleSprite, originalVColors, false);
        }

        void ApplyPanelStyle(ThemeAsset theme, ScrollRectStyleParameters style)
        {
            if (!panelImage) return;

            if (!string.IsNullOrEmpty(style.PanelSpriteKey))
            {
                panelImage.sprite = theme.GetSprite(style.PanelSpriteKey);
                panelImage.type = theme.GetSpriteType(style.PanelSpriteKey);
            }

            if (!string.IsNullOrEmpty(style.PanelColorKey))
                panelImage.color = theme.GetColor(style.PanelColorKey);
        }

        void ApplyScrollbarStyle(Scrollbar scrollbar, ThemeAsset theme,
            ScrollRectStyleParameters style, Image background, Image handle,
            Sprite originalBgSprite, Sprite originalHandleSprite,
            ColorBlock originalColors, bool isHorizontal)
        {
            // Apply sprites
            ApplySprites(theme, style, background, handle,
                originalBgSprite, originalHandleSprite, isHorizontal);

            // Set transition mode and apply specific settings
            scrollbar.transition = style.ScrollbarTransition;

            switch (style.ScrollbarTransition)
            {
                case Selectable.Transition.ColorTint:
                    scrollbar.spriteState = new SpriteState();
                    ApplyColorTintSettings(scrollbar, theme, style, originalColors);
                    break;

                case Selectable.Transition.SpriteSwap:
                    scrollbar.colors = GetDefaultColorBlock();
                    ApplySpriteSwapSettings(scrollbar, theme, style);
                    break;

                case Selectable.Transition.None:
                    scrollbar.spriteState = new SpriteState();
                    scrollbar.colors = GetDefaultColorBlock();
                    break;
            }
        }

        void ApplySprites(ThemeAsset theme, ScrollRectStyleParameters style,
            Image background, Image handle, Sprite originalBgSprite,
            Sprite originalHandleSprite, bool isHorizontal)
        {
            if (background)
            {
                var bgKey = isHorizontal ? style.HBackgroundSpriteKey : style.VBackgroundSpriteKey;
                var bgSprite = !string.IsNullOrEmpty(bgKey) ? theme.GetSprite(bgKey) : originalBgSprite;
                background.sprite = bgSprite;

                if (!string.IsNullOrEmpty(bgKey))
                    background.type = theme.GetSpriteType(bgKey);
            }

            if (handle)
            {
                var handleKey = isHorizontal ? style.HHandleSpriteKey : style.VHandleSpriteKey;
                var handleSprite = !string.IsNullOrEmpty(handleKey) ? theme.GetSprite(handleKey) : originalHandleSprite;
                handle.sprite = handleSprite;

                if (!string.IsNullOrEmpty(handleKey))
                    handle.type = theme.GetSpriteType(handleKey);
            }
        }

        void ApplyColorTintSettings(Scrollbar scrollbar, ThemeAsset theme,
            ScrollRectStyleParameters style, ColorBlock originalColors)
        {
            var colors = originalColors;
            colors.colorMultiplier = style.ColorMultiplier;
            colors.fadeDuration = style.FadeDuration;

            if (!string.IsNullOrEmpty(style.NormalColorKey))
                colors.normalColor = theme.GetColor(style.NormalColorKey);

            if (!string.IsNullOrEmpty(style.HighlightedColorKey))
                colors.highlightedColor = theme.GetColor(style.HighlightedColorKey);

            if (!string.IsNullOrEmpty(style.PressedColorKey))
                colors.pressedColor = theme.GetColor(style.PressedColorKey);

            if (!string.IsNullOrEmpty(style.SelectedColorKey))
                colors.selectedColor = theme.GetColor(style.SelectedColorKey);

            if (!string.IsNullOrEmpty(style.DisabledColorKey))
                colors.disabledColor = theme.GetColor(style.DisabledColorKey);

            scrollbar.colors = colors;
        }

        void ApplySpriteSwapSettings(Scrollbar scrollbar, ThemeAsset theme,
            ScrollRectStyleParameters style)
        {
            var spriteState = new SpriteState();

            if (!string.IsNullOrEmpty(style.HighlightedHandleSpriteKey))
                spriteState.highlightedSprite = theme.GetSprite(style.HighlightedHandleSpriteKey);

            if (!string.IsNullOrEmpty(style.PressedHandleSpriteKey))
                spriteState.pressedSprite = theme.GetSprite(style.PressedHandleSpriteKey);

            if (!string.IsNullOrEmpty(style.SelectedHandleSpriteKey))
                spriteState.selectedSprite = theme.GetSprite(style.SelectedHandleSpriteKey);

            if (!string.IsNullOrEmpty(style.DisabledHandleSpriteKey))
                spriteState.disabledSprite = theme.GetSprite(style.DisabledHandleSpriteKey);

            scrollbar.spriteState = spriteState;
        }

        static ColorBlock GetDefaultColorBlock() => new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = Color.white,
            pressedColor = Color.white,
            selectedColor = Color.white,
            disabledColor = Color.white,
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
    }
}