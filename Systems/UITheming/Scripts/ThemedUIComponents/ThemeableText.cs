using TMPro;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>Applies a <see cref="TextStyleParameters"/> module to a TMP_Text.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class ThemeableText : MonoBehaviour, IThemeable
    {
        [ThemeKey(typeof(TextStyleParameters))]
        [SerializeField] private string styleKey = "body";
        [SerializeField] bool CustomFontSize = false;
        [SerializeField] bool CustomLetterSpacing = false;

        TMP_Text text;

        void Awake() => CacheReferences();

        void OnEnable()
        {
            this.SubscribeEvent<ThemeChangedEvent>(e => ApplyTheme(e.Theme));
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        void CacheReferences() => text = text ? text : GetComponent<TMP_Text>();

        /// <inheritdoc/>
        public void ApplyTheme(ThemeAsset theme)
        {
            CacheReferences();
            if (text == null)
                return;

            if (theme == null ||
                !theme.TryGetStyle(styleKey, out TextStyleParameters style))
                return;

            text.color = theme.GetColor(style.ColorKey);
            if (!CustomFontSize)
                text.fontSize = style.FontSize;
            text.fontStyle = style.FontStyle;
            if (!CustomLetterSpacing)
                text.characterSpacing = style.LetterSpacing;

            var font = theme.GetFont(style.FontKey);
            if (font) text.font = font;
        }
    }
}
