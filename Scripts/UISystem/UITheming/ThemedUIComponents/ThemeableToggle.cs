using JG.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Theming
{
    /// <summary>Applies a <see cref="ToggleStyleParameters"/> module to a Toggle.</summary>
    [RequireComponent(typeof(Toggle))]
    public sealed class ThemeableToggle : MonoBehaviour, IThemeable
    {
        [SerializeField] private string styleKey = "Default";

        Toggle toggle;
        Image background;
        Image checkmark;
        EventBinding<ThemeChangedEvent> binding;

        void Awake()
        {
            toggle = GetComponent<Toggle>();
            background = toggle.targetGraphic as Image;
            checkmark = toggle.graphic as Image;
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
            if (theme == null ||
                !theme.TryGetStyle(styleKey, out ToggleStyleParameters s)) return;

            // ─── Static sprites & colours ------------------------------------------
            if (background)
            {
                if (!string.IsNullOrEmpty(s.BackgroundSpriteKey))
                {
                    background.sprite = theme.GetSprite(s.BackgroundSpriteKey);
                    background.type = theme.GetSpriteType(s.BackgroundSpriteKey);
                }
                if (!string.IsNullOrEmpty(s.BackgroundColorKey))
                    background.color = theme.GetColor(s.BackgroundColorKey);
            }

            if (checkmark)
            {
                if (!string.IsNullOrEmpty(s.CheckmarkSpriteKey))
                    checkmark.sprite = theme.GetSprite(s.CheckmarkSpriteKey);

                if (!string.IsNullOrEmpty(s.CheckmarkColorKey))
                    checkmark.color = theme.GetColor(s.CheckmarkColorKey);
            }

            // ─── Interaction / transition ------------------------------------------
            toggle.transition = s.Transition;

            if (s.Transition == Selectable.Transition.SpriteSwap)
            {
                var swap = s.SpriteSwap;
                toggle.spriteState = new SpriteState
                {
                    highlightedSprite = LoadSprite(theme, swap.highlightedSpriteKey),
                    pressedSprite = LoadSprite(theme, swap.pressedSpriteKey),
                    selectedSprite = LoadSprite(theme, swap.selectedSpriteKey),
                    disabledSprite = LoadSprite(theme, swap.disabledSpriteKey)
                };
            }
            else // ColorTint (default & most common)
            {
                var tint = s.ColorTint;
                var cb = toggle.colors;          // start from current so we don't reset extra fields

                if (!string.IsNullOrEmpty(tint.normalColorKey))
                    cb.normalColor = theme.GetColor(tint.normalColorKey);
                if (!string.IsNullOrEmpty(tint.highlightedColorKey))
                    cb.highlightedColor = theme.GetColor(tint.highlightedColorKey);
                if (!string.IsNullOrEmpty(tint.pressedColorKey))
                    cb.pressedColor = theme.GetColor(tint.pressedColorKey);
                if (!string.IsNullOrEmpty(tint.selectedColorKey))
                    cb.selectedColor = theme.GetColor(tint.selectedColorKey);
                if (!string.IsNullOrEmpty(tint.disabledColorKey))
                    cb.disabledColor = theme.GetColor(tint.disabledColorKey);

                cb.colorMultiplier = tint.colorMultiplier == 0 ? 1 : tint.colorMultiplier;
                cb.fadeDuration = tint.fadeDuration == 0 ? 0.1f : tint.fadeDuration;

                toggle.colors = cb;
            }
        }

        // helper ---------------------------------------------------------------------
        static Sprite LoadSprite(ThemeAsset t, string key) =>
            string.IsNullOrEmpty(key) ? null : t.GetSprite(key);
    }
}