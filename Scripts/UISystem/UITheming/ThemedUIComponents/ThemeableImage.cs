using UnityEngine;
using UnityEngine.UI;
using JG.Tools;

namespace UI.Theming
{
    /// <summary>
    /// Re-skins a UnityEngine.UI.Image when the active theme changes.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ThemeableImage : MonoBehaviour, IThemeable
    {
        [Header("Sprite")]
        [Tooltip("If set, the sprite with the matching key in ThemeAsset will be used.")]
        [SerializeField] private string spriteKey;

        [Header("Color")]
        [SerializeField] private ColorRole colorRole = ColorRole.None;

        private Image image;
        private EventBinding<ThemeChangedEvent> binding;

        // ---------------------------------------------------------------------

        void Awake()
        {
            image = GetComponent<Image>();
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
            if (!string.IsNullOrEmpty(spriteKey) && theme.TryGetSprite(spriteKey, out Sprite s))
            {
                image.sprite = s;
            }

            switch (colorRole)
            {
                case ColorRole.Primary: image.color = theme.PrimaryColor; break;
                case ColorRole.Secondary: image.color = theme.SecondaryColor; break;
                case ColorRole.Background: image.color = theme.BackgroundColor; break;
                case ColorRole.Text: image.color = theme.TextColor; break;
            }
        }
    }
}
