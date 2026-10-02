using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Masked viewport for tooltip content. Reports the full content size to the parent layout
    /// (so the tooltip grows naturally) but can be squeezed below it, in which case the content
    /// becomes scrollable via the owning player's mouse wheel or right stick.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(RectMask2D))]
    public sealed class TooltipScrollViewport : UIBehaviour, ILayoutGroup, ILayoutElement
    {
        [SerializeField] private RectTransform _content;

        [Header("Input")]
        [SerializeField, Min(0f), Tooltip("Scroll distance per mouse wheel notch, in layer units.")]
        private float _wheelStep = 60f;
        [SerializeField, Min(0f), Tooltip("Scroll speed at full right stick deflection, in layer units per second.")]
        private float _stickSpeed = 900f;
        [SerializeField, Range(0f, 0.9f)]
        private float _stickDeadzone = 0.25f;

        private readonly List<InputDevice> _deviceBuffer = new();
        private TooltipPlayerContext _playerContext;
        private bool _inputEnabled;
        private float _scroll;

        public RectTransform Content => _content;
        public bool CanScroll => MaxScroll > 0.5f;

        private RectTransform Rect => (RectTransform)transform;
        private float MaxScroll => _content == null ? 0f : Mathf.Max(0f, _content.rect.height - Rect.rect.height);

        internal static TooltipScrollViewport Create(RectTransform parent, HorizontalOrVerticalLayoutGroup template)
        {
            var viewportGo = new GameObject("ScrollViewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.layer = parent.gameObject.layer;
            var viewportRect = (RectTransform)viewportGo.transform;
            viewportRect.SetParent(parent, false);
            viewportRect.SetAsLastSibling();

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGo.layer = parent.gameObject.layer;
            var content = (RectTransform)contentGo.transform;
            content.SetParent(viewportRect, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            if (template != null)
            {
                layout.spacing = template.spacing;
                layout.childAlignment = template.childAlignment;
            }

            var viewport = viewportGo.AddComponent<TooltipScrollViewport>();
            viewport._content = content;
            return viewport;
        }

        public void Bind(TooltipPlayerContext playerContext, bool inputEnabled = true)
        {
            _playerContext = playerContext;
            _inputEnabled = inputEnabled;
            ResetScroll();
        }

        public void Unbind()
        {
            _playerContext = default;
            _inputEnabled = false;
            ResetScroll();
        }

        public void ResetScroll()
        {
            _scroll = 0f;
            ApplyScroll();
        }

        public void ScrollBy(float delta)
        {
            _scroll += delta;
            ApplyScroll();
        }

        private void ApplyScroll()
        {
            if (_content == null)
            {
                return;
            }

            _scroll = Mathf.Clamp(_scroll, 0f, MaxScroll);
            _content.anchoredPosition = new Vector2(0f, _scroll);
        }

        private void Update()
        {
            if (!_inputEnabled || !CanScroll)
            {
                return;
            }

            float delta = ReadScrollInput(Time.unscaledDeltaTime);
            if (!Mathf.Approximately(delta, 0f))
            {
                ScrollBy(delta);
            }
        }

        // Positive result scrolls down (content moves up). Only reads devices paired to the
        // tooltip's player so other couch-coop players can't scroll someone else's tooltip.
        private float ReadScrollInput(float deltaTime)
        {
            float delta = 0f;
            CollectDevices();

            for (int i = 0; i < _deviceBuffer.Count; i++)
            {
                switch (_deviceBuffer[i])
                {
                    case Mouse mouse:
                        float wheel = mouse.scroll.ReadValue().y;
                        if (!Mathf.Approximately(wheel, 0f))
                        {
                            delta -= Mathf.Sign(wheel) * _wheelStep;
                        }
                        break;

                    case Gamepad gamepad:
                        float stick = gamepad.rightStick.ReadValue().y;
                        if (Mathf.Abs(stick) > _stickDeadzone)
                        {
                            float t = (Mathf.Abs(stick) - _stickDeadzone) / (1f - _stickDeadzone);
                            delta -= Mathf.Sign(stick) * t * _stickSpeed * deltaTime;
                        }
                        break;
                }
            }

            _deviceBuffer.Clear();
            return delta;
        }

        private void CollectDevices()
        {
            var playerInput = _playerContext.PlayerInput;
            if (playerInput == null && _playerContext.MultiplayerEventSystem != null && _playerContext.MultiplayerEventSystem.playerRoot != null)
            {
                playerInput = _playerContext.MultiplayerEventSystem.playerRoot.GetComponentInParent<PlayerInput>();
            }

            if (playerInput != null)
            {
                foreach (var device in playerInput.devices)
                {
                    _deviceBuffer.Add(device);
                }
                return;
            }

            // No player binding (single player / debug): fall back to the most recent devices.
            if (Mouse.current != null) _deviceBuffer.Add(Mouse.current);
            if (Gamepad.current != null) _deviceBuffer.Add(Gamepad.current);
        }

        #region Layout

        public void SetLayoutHorizontal()
        {
            if (_content == null)
            {
                return;
            }

            _content.sizeDelta = new Vector2(0f, _content.sizeDelta.y);
        }

        public void SetLayoutVertical()
        {
            if (_content == null)
            {
                return;
            }

            _content.sizeDelta = new Vector2(0f, LayoutUtility.GetPreferredHeight(_content));
            ApplyScroll();
        }

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }

        public float minWidth => _content != null ? LayoutUtility.GetMinWidth(_content) : 0f;
        public float preferredWidth => _content != null ? LayoutUtility.GetPreferredWidth(_content) : 0f;
        public float flexibleWidth => -1f;
        public float minHeight => 0f;
        public float preferredHeight => _content != null ? LayoutUtility.GetPreferredHeight(_content) : 0f;
        public float flexibleHeight => -1f;
        public float maxWidth => -1f;
        public float maxHeight => -1f;
        public int layoutPriority => 1;

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            SetDirty();
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            ApplyScroll();
        }

        private void SetDirty()
        {
            if (IsActive())
            {
                LayoutRebuilder.MarkLayoutForRebuild(Rect);
            }
        }

        #endregion
    }
}
