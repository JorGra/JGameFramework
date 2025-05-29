using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JG.Tools;

namespace UI.Theming
{
    /// <summary>
    /// Re-skins a UGUI <see cref="Toggle"/> with ThemeAsset colours **and** sprites.
    /// The label is styled by a separate <see cref="ThemeableText"/> component.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ThemeableToggle : MonoBehaviour, IThemeable
    {
        // ----------------------------------------------------- Sprite keys -----
        [Header("Sprite Keys (optional)")]
        [Tooltip("Sprite when toggle is ON and interactable.")]
        [SerializeField] private string bgOnSpriteKey;
        [Tooltip("Sprite when toggle is OFF and interactable.")]
        [SerializeField] private string bgOffSpriteKey;
        [Tooltip("Sprite when toggle is DISABLED.")]
        [SerializeField] private string bgDisabledSpriteKey;
        [Tooltip("Sprite for the check-mark glyph (shown only when IsOn).")]
        [SerializeField] private string checkmarkSpriteKey;

        // ---------------------------------------------------- Colour roles -----
        [Header("Colour Roles")]
        [SerializeField] private ColorRole onColor = ColorRole.Primary;
        [SerializeField] private ColorRole offColor = ColorRole.Secondary;
        [SerializeField] private ColorRole disabledColor = ColorRole.Background;
        [SerializeField] private ColorRole checkColor = ColorRole.Text;

        // ------------------------------------------------ Graphic references ---
        [Header("Graphic References")]
        [SerializeField] private Image background;          // “Background”
        [SerializeField] private Image checkmark;           // “Background/Checkmark”
        [Tooltip("Label MUST have a ThemeableText component attached.")]
        [SerializeField] private ThemeableText label;         // The label

        // ------------------------------------------------------ internals ------
        Toggle toggle;
        EventBinding<ThemeChangedEvent> binding;
        ThemeAsset theme;     // cached for sprite look-ups

        // ----------------------------------------------------------------------

        void Awake()
        {
            toggle = GetComponent<Toggle>();

            // Auto-assign common children if left blank
            if (!background) background = GetComponentInChildren<Image>(true);
            if (!checkmark)
            {
                var ck = background ? background.transform.Find("Checkmark") : null;
                checkmark = ck ? ck.GetComponent<Image>() : null;
            }
            if (!label) label = GetComponentInChildren<ThemeableText>(true);
        }

        void OnEnable()
        {
            binding = new EventBinding<ThemeChangedEvent>(OnThemeChanged);
            EventBus<ThemeChangedEvent>.Register(binding);

            toggle.onValueChanged.AddListener(_ => RefreshVisuals());

            theme = ThemeManager.Instance?.CurrentTheme;
            RefreshVisuals();
        }

        void OnDisable()
        {
            EventBus<ThemeChangedEvent>.Deregister(binding);
            toggle.onValueChanged.RemoveAllListeners();
        }

        // ---------------------------------------------------------------------

        void OnThemeChanged(ThemeChangedEvent e)
        {
            theme = e.Theme;
            RefreshVisuals();
        }

        /// <inheritdoc/>
        public void ApplyTheme(ThemeAsset newTheme)
        {
            theme = newTheme;
            RefreshVisuals();
        }

        // ---------------------------------------------------------------------
        void RefreshVisuals()
        {
            if (theme == null) return;

            bool interactable = toggle.interactable;
            bool isOn = toggle.isOn;

            //------------------ colours ----------------
            Color bgCol = Resolve(interactable
                                             ? (isOn ? onColor : offColor)
                                             : disabledColor);
            Color ckCol = Resolve(checkColor);

            if (background) background.color = bgCol;
            if (checkmark) checkmark.color = ckCol;

            //------------------ sprites ---------------
            if (background)
            {
                string key = interactable
                             ? (isOn ? bgOnSpriteKey : bgOffSpriteKey)
                             : bgDisabledSpriteKey;

                if (!string.IsNullOrWhiteSpace(key) &&
                    theme.TryGetSprite(key, out Sprite s))
                {
                    background.sprite = s;
                }
            }

            if (checkmark && !string.IsNullOrWhiteSpace(checkmarkSpriteKey) &&
                theme.TryGetSprite(checkmarkSpriteKey, out Sprite ckSprite))
            {
                checkmark.sprite = ckSprite;
            }

            //------------------ check-mark vis --------
            if (checkmark) checkmark.enabled = isOn;
        }

        // ---------------------------------------------------------------------
        Color Resolve(ColorRole role)
        {
            return role switch
            {
                ColorRole.Primary => theme.PrimaryColor,
                ColorRole.Secondary => theme.SecondaryColor,
                ColorRole.Background => theme.BackgroundColor,
                ColorRole.Text => theme.TextColor,
                _ => Color.white
            };
        }
    }
}
