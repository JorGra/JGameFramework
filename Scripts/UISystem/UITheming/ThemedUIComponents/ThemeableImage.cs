using JG.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Theming
{
    /// <summary>
    /// Applies an <see cref="ImageStyleParameters"/> module to a UI Image, including
    /// the Pixels-Per-Unit override defined in the theme's Sprite entry.
    /// </summary>
    public sealed class ThemeableImage : MonoBehaviour, IThemeable
    {
        [SerializeField] private string styleKey = "Icon";

        Image image;

        void Awake() => image = GetComponent<Image>();

        void OnEnable()
        {
            this.SubscribeEvent<ThemeChangedEvent>(e => ApplyTheme(e.Theme));

            if (ThemeManager.Instance != null)
                ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        // ---------------------------------------------------------------------
        // IThemeable
        // ---------------------------------------------------------------------
        public void ApplyTheme(ThemeAsset theme)
        {
            if (theme == null ||
                !theme.TryGetStyle(styleKey, out ImageStyleParameters style))
                return;

            if (!string.IsNullOrEmpty(style.SpriteKey))
            {
                Sprite sprite = theme.GetSprite(style.SpriteKey);
                image.sprite = sprite;
                image.type = theme.GetSpriteType(style.SpriteKey);

                float desiredPPU = theme.GetSpritePixelPerUnit(style.SpriteKey);

                if (sprite != null && sprite.pixelsPerUnit > 0f)
                    image.pixelsPerUnitMultiplier = desiredPPU;
                else
                    image.pixelsPerUnitMultiplier = 1f;   // safe fallback
            }

            if (!string.IsNullOrEmpty(style.TintColorKey))
                image.color = theme.GetColor(style.TintColorKey);
        }
    }
}
