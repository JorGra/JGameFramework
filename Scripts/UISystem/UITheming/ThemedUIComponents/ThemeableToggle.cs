using JG.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Theming
{
    /// <summary>Applies a <see cref="ToggleStyleParameters"/> module to a Toggle.</summary>
    [RequireComponent(typeof(Toggle))]
    public sealed class ThemeableToggle : MonoBehaviour, IThemeable
    {
        [SerializeField] private string styleKey = "Toggle";

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
                !theme.TryGetStyle(styleKey, out ToggleStyleParameters style))
                return;

            // sprites
            if (background && !string.IsNullOrEmpty(style.BackgroundSpriteKey))
                background.sprite = theme.GetSprite(style.BackgroundSpriteKey);

            if (checkmark && !string.IsNullOrEmpty(style.CheckmarkSpriteKey))
                checkmark.sprite = theme.GetSprite(style.CheckmarkSpriteKey);

            // colours
            var cb = toggle.colors;
            cb.normalColor = theme.GetColor(style.OffColorKey);
            cb.highlightedColor = cb.normalColor * 1.1f;
            cb.selectedColor = theme.GetColor(style.OnColorKey);
            cb.pressedColor = cb.selectedColor * 0.9f;
            cb.disabledColor = theme.GetColor(style.DisabledColorKey);
            toggle.colors = cb;
        }
    }
}
