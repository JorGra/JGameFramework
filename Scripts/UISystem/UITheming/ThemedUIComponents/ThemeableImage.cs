using JG.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Theming
{
    /// <summary>Applies an <see cref="ImageStyleParameters"/> module to a UI Image.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class ThemeableImage : MonoBehaviour, IThemeable
    {
        [SerializeField] string styleKey = "Icon";

        Image image;
        EventBinding<ThemeChangedEvent> binding;

        void Awake() => image = GetComponent<Image>();

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
                !theme.TryGetStyle(styleKey, out ImageStyleParameters style))
                return;

            if (!string.IsNullOrEmpty(style.SpriteKey))
            {
                image.sprite = theme.GetSprite(style.SpriteKey);
                image.type = theme.GetSpriteType(style.SpriteKey);
            }

            if (!string.IsNullOrEmpty(style.TintColorKey))
                image.color = theme.GetColor(style.TintColorKey);
        }
    }
}
