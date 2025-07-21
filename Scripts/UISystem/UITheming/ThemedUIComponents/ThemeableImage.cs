using JG.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Theming
{
    /// <summary>
    /// Applies an <see cref="ImageStyleParameters"/> module to a UI Image, including
    /// the Pixels-Per-Unit override defined in the theme’s Sprite entry.
    /// </summary>
    //[RequireComponent(typeof(Image))]
    public sealed class ThemeableImage : MonoBehaviour, IThemeable
    {
        [SerializeField] private string styleKey = "Icon";

        Image image;
        EventBinding<ThemeChangedEvent> binding;

        void Awake() => image = GetComponent<Image>();

        void OnEnable()
        {
            binding = new EventBinding<ThemeChangedEvent>(e => ApplyTheme(e.Theme));
            EventBus<ThemeChangedEvent>.Register(binding);

            if (ThemeManager.Instance != null)
                ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        void OnDisable() => EventBus<ThemeChangedEvent>.Deregister(binding);

        // ---------------------------------------------------------------------
        // IThemeable
        // ---------------------------------------------------------------------
        public void ApplyTheme(ThemeAsset theme)
        {
            if (theme == null ||
                !theme.TryGetStyle(styleKey, out ImageStyleParameters style))
                return;

            // ─── Sprite & type ────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(style.SpriteKey))
            {
                Sprite sprite = theme.GetSprite(style.SpriteKey);
                image.sprite = sprite;
                image.type = theme.GetSpriteType(style.SpriteKey);

                // ─── Pixels-Per-Unit override ─────────────────────────────────
                // Theme stores the desired *absolute* PPU; Image expects a *multiplier*.
                float desiredPPU = theme.GetSpritePixelPerUnit(style.SpriteKey);

                if (sprite != null && sprite.pixelsPerUnit > 0f)
                    image.pixelsPerUnitMultiplier = desiredPPU;// / sprite.pixelsPerUnit;
                else
                    image.pixelsPerUnitMultiplier = 1f;   // safe fallback
            }

            // ─── Tint colour ─────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(style.TintColorKey))
                image.color = theme.GetColor(style.TintColorKey);
        }
    }
}
