using UnityEngine;
using UnityEngine.UI;
using JG.Tools;
using TMPro;

namespace UI.Theming
{
    /// <summary>
    /// Updates a Text component’s color and (optionally) font when the theme changes.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ThemeableText : MonoBehaviour, IThemeable
    {
        [SerializeField] private ColorRole colorRole = ColorRole.Text;
        [SerializeField] private bool overrideFont = true;

        private TMP_Text text;
        private EventBinding<ThemeChangedEvent> binding;

        // ---------------------------------------------------------------------

        void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            binding = new EventBinding<ThemeChangedEvent>(OnThemeChanged);
            EventBus<ThemeChangedEvent>.Register(binding);

            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        void OnDisable()
        {
            EventBus<ThemeChangedEvent>.Deregister(binding);
        }

        // ---------------------------------------------------------------------

        void OnThemeChanged(ThemeChangedEvent e) => ApplyTheme(e.Theme);

        /// <inheritdoc/>
        public void ApplyTheme(ThemeAsset theme)
        {
            if (!text)
                return;

            switch (colorRole)
            {
                case ColorRole.Primary: text.color = theme.PrimaryColor; break;
                case ColorRole.Secondary: text.color = theme.SecondaryColor; break;
                case ColorRole.Background: text.color = theme.BackgroundColor; break;
                case ColorRole.Text: text.color = theme.TextColor; 
                break;
            }

            if (overrideFont && theme.DefaultFont != null)
            {
                text.font = theme.DefaultFont;
            }
        }
    }
}
