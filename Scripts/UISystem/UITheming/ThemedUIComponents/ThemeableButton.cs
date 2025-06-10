// ───────────────────────────────────────────────────────── ThemeableButton.cs
using JG.Tools;                  // ↳ whatever namespace holds EventBus / ThemeChangedEvent
using UnityEngine;
using UnityEngine.UI;

namespace UI.Theming
{
    /// <summary>
    /// Applies <see cref="ButtonStyleParameters"/> to a Unity UI Button at runtime.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ThemeableButton : MonoBehaviour, IThemeable
    {
        [SerializeField] private string styleKey = "Default";

        Button button;
        Image targetImage;
        EventBinding<ThemeChangedEvent> binding;

        // ────────────────────────────────────────────────────────────────── setup
        void Awake()
        {
            button = GetComponent<Button>();
            // Try the explicit targetGraphic first, fall back to root Image
            targetImage = button.targetGraphic as Image ?? GetComponent<Image>();
        }

        void OnEnable()
        {
            binding = new EventBinding<ThemeChangedEvent>(e => ApplyTheme(e.Theme));
            EventBus<ThemeChangedEvent>.Register(binding);
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }
        void OnDisable() => EventBus<ThemeChangedEvent>.Deregister(binding);

        // ─────────────────────────────────────────────────────────── theming API
        public void ApplyTheme(ThemeAsset theme)
        {
            if (theme == null ||
                !theme.TryGetStyle(styleKey, out ButtonStyleParameters p))
                return;

            // Background sprite / colour ----------------------------------------
            if (targetImage)
            {
                if (!string.IsNullOrEmpty(p.BackgroundSpriteKey))
                {
                    targetImage.sprite = theme.GetSprite(p.BackgroundSpriteKey);
                    targetImage.type = theme.GetSpriteType(p.BackgroundSpriteKey);
                }
                if (!string.IsNullOrEmpty(p.BackgroundColorKey))
                    targetImage.color = theme.GetColor(p.BackgroundColorKey);
            }

            // Transition mode & optional sprite swap ----------------------------
            button.transition = p.Transition;

            if (p.Transition == Selectable.Transition.SpriteSwap)
            {
                var swap = p.SpriteSwap;
                var s = new SpriteState
                {
                    highlightedSprite = LoadSprite(theme, swap.highlightedSpriteKey),
                    pressedSprite = LoadSprite(theme, swap.pressedSpriteKey),
                    selectedSprite = LoadSprite(theme, swap.selectedSpriteKey),
                    disabledSprite = LoadSprite(theme, swap.disabledSpriteKey)
                };
                button.spriteState = s;
            }
        }

        // helper ---------------------------------------------------------------
        static Sprite LoadSprite(ThemeAsset t, string key) =>
            string.IsNullOrEmpty(key) ? null : t.GetSprite(key);
    }
}
