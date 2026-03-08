using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.GameContent.Debugging
{
    public sealed class DebugConsoleLogEntryView : MonoBehaviour
    {
        [SerializeField] TMP_Text messageText;
        [SerializeField] TMP_Text stacktraceText;
        [SerializeField] TMP_Text countBadge;
        [SerializeField] Image background;
        [SerializeField] Image severityIcon;
        [SerializeField] Button expandButton;
        [SerializeField] LayoutElement layoutElement;

        static readonly Color ErrorColor = new(1f, 0.35f, 0.35f);
        static readonly Color WarningColor = new(1f, 0.85f, 0.25f);
        static readonly Color InfoColor = new(0.85f, 0.85f, 0.85f);

        bool _expanded;
        System.Action<DebugConsoleLogEntryView> _onClickExpand;

        void Awake()
        {
            if (expandButton != null)
                expandButton.onClick.AddListener(OnExpandClicked);
            SetExpanded(false);
        }

        Sprite _errorSprite;
        Sprite _warningSprite;
        Sprite _infoSprite;

        public void SetIcons(Sprite error, Sprite warning, Sprite info)
        {
            _errorSprite = error;
            _warningSprite = warning;
            _infoSprite = info;
        }

        public void SetExpandCallback(System.Action<DebugConsoleLogEntryView> callback)
        {
            _onClickExpand = callback;
        }

        public void Bind(ConsoleLogEntry entry, bool isExpanded, Color bgColor)
        {
            if (messageText != null)
            {
                messageText.text = entry.Message;
                messageText.color = ColorForType(entry.Type);
            }

            if (stacktraceText != null)
                stacktraceText.text = entry.Stacktrace;

            if (countBadge != null)
            {
                countBadge.gameObject.SetActive(entry.Count > 1);
                countBadge.text = $"x{entry.Count}";
            }

            if (background != null)
                background.color = bgColor;

            if (severityIcon != null)
            {
                severityIcon.sprite = SpriteForType(entry.Type);
                severityIcon.color = ColorForType(entry.Type);
            }

            SetExpanded(isExpanded);
        }

        public void SetExpanded(bool expanded)
        {
            _expanded = expanded;
            if (stacktraceText != null)
                stacktraceText.gameObject.SetActive(expanded && !string.IsNullOrEmpty(stacktraceText.text));
        }

        void OnExpandClicked()
        {
            _onClickExpand?.Invoke(this);
        }

        Sprite SpriteForType(LogType type) => type switch
        {
            LogType.Error or LogType.Exception or LogType.Assert => _errorSprite,
            LogType.Warning => _warningSprite,
            _ => _infoSprite
        };

        static Color ColorForType(LogType type) => type switch
        {
            LogType.Error or LogType.Exception or LogType.Assert => ErrorColor,
            LogType.Warning => WarningColor,
            _ => InfoColor
        };
    }
}
