using JG.Tools;
using TMPro;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>Applies a <see cref="TextStyleParameters"/> module to a TMP_Text.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class ThemeableText : MonoBehaviour, IThemeable
    {
        [SerializeField] private string styleKey = "body";
        [SerializeField] bool CustomFontSize = false;

        TMP_Text text;
        EventBinding<ThemeChangedEvent> binding;

        void Awake() => text = GetComponent<TMP_Text>();

        void OnEnable()
        {
            binding = new EventBinding<ThemeChangedEvent>(e => ApplyTheme(e.Theme));
            EventBus<ThemeChangedEvent>.Register(binding);
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        void OnDisable() => EventBus<ThemeChangedEvent>.Deregister(binding);

        /// <inheritdoc/>
        public void ApplyTheme(ThemeAsset theme)
        {
            if (theme == null ||
                !theme.TryGetStyle(styleKey, out TextStyleParameters style))
                return;

            text.color = theme.GetColor(style.ColorKey);
            if (!CustomFontSize)
                text.fontSize = style.FontSize;
            text.fontStyle = style.FontStyle;

            var font = theme.GetFont(style.FontKey);
            if (font) text.font = font;
        }
    }
}
