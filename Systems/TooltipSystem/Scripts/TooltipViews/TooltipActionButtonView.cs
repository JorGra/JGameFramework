using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
#endif

namespace JGameFramework.UI.Tooltips
{
    [RequireComponent(typeof(Button))]
    public class TooltipActionButtonView : Button
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Image _icon;

        private TooltipActionData _actionData;
        private TooltipHandle _handle;
        private TooltipPlayerContext _playerContext;

        protected override void Awake()
        {
            base.Awake();
            onClick.RemoveAllListeners();
            onClick.AddListener(InvokeAction);
        }

        internal void Initialize(TooltipActionData actionData, TooltipHandle handle, TooltipPlayerContext playerContext)
        {
            _actionData = actionData;
            _handle = handle;
            _playerContext = playerContext;

            if (_label != null)
            {
                _label.text = actionData.Label ?? string.Empty;
            }

            if (_icon != null)
            {
                _icon.sprite = actionData.Icon;
                _icon.enabled = actionData.Icon != null;
            }

            interactable = actionData.Interactable;
        }

        internal void Release()
        {
            onClick.RemoveAllListeners();
            onClick.AddListener(InvokeAction);
            _actionData = null;
            _handle = default;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!IsAllowed(eventData))
            {
                return;
            }

            var view = GetComponentInParent<TooltipView>();
            if (view != null)
            {
                view.NotifyActionClickedByPointer();
            }

            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (!IsAllowed(eventData))
            {
                return;
            }

            base.OnSubmit(eventData);
        }

        private bool IsAllowed(BaseEventData eventData)
        {
            if (!interactable)
            {
                return false;
            }

            if (!_playerContext.IsValid)
            {
                return true;
            }

            if (eventData == null)
            {
                return true;
            }

            return _playerContext.MatchesEvent(eventData);
        }

        private void InvokeAction()
        {
            if (_actionData == null || !_handle.IsValid)
            {
                return;
            }

            _actionData.Invoke(_handle, _playerContext);
        }
    }
}

