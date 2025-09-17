using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace JGameFramework.UI.Tooltips
{
    public sealed class TooltipView : MonoBehaviour
    {
        [Header("Structure")]
        [SerializeField] private RectTransform _root;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private RectTransform _actionsRoot;
        [SerializeField] private LayoutGroup _actionsLayout;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Canvas _overrideCanvas;

        private TooltipSystemRoot _system;
        private TooltipRequest _request;
        private TooltipHandle _handle;
        private readonly List<TooltipContentViewBase> _spawnedContent = new();
        private readonly List<TooltipActionButtonView> _spawnedActions = new();
        private Vector2 _currentOffset;
        private bool _isVisible = true;
        private Vector3? _worldPosition;
        private bool _isFollowing;

        public TooltipPlayerContext PlayerContext => _request.PlayerContext;
        public object Tag { get; private set; }

        internal void Initialize(TooltipRequest request, TooltipHandle handle)
        {
            _system = TooltipSystemRoot.Instance;
            _request = request;
            _handle = handle;
            _worldPosition = request.WorldPosition;
            _isFollowing = request.FollowTarget;
            Tag = request.Tag;

            if (_root == null)
            {
                _root = transform as RectTransform;
            }

            if (_contentRoot == null)
            {
                _contentRoot = _root;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _currentOffset = _system.DefaultOffset + request.Offset;
            _root.pivot = request.Pivot;

            SetupSorting(request.SortingOffset);
            BuildContent();
            UpdatePosition(true);
            SetVisibility(true);
        }

        internal void Release()
        {
            ClearContent();
            ClearActions();
            _system = null;
            _handle = default;
            _request = default;
            Tag = null;
            _worldPosition = null;
        }

        private void SetupSorting(int sortingOffset)
        {
            if (_overrideCanvas == null)
            {
                _overrideCanvas = GetComponent<Canvas>();
            }

            if (_overrideCanvas != null)
            {
                _overrideCanvas.overrideSorting = true;
                int baseOrder = _system.Canvas != null ? _system.Canvas.sortingOrder : 0;
                _overrideCanvas.sortingOrder = baseOrder + sortingOffset;
            }
        }

        private void BuildContent()
        {
            ClearContent();
            ClearActions();

            var contentList = _request.Content;
            if (contentList != null)
            {
                for (int i = 0; i < contentList.Count; i++)
                {
                    var data = contentList[i];
                    if (data == null) continue;
                    var prefab = _system.ResolveViewPrefab(data.GetType());
                    if (prefab == null) continue;
                    var instance = Instantiate(prefab, _contentRoot);
                    instance.Bind(data, new TooltipBindingContext(_handle, _system, _request.PlayerContext));
                    _spawnedContent.Add(instance);
                }
            }

            var actions = _request.Actions;
            if (actions != null && actions.Count > 0)
            {
                if (_actionsRoot != null)
                {
                    _actionsRoot.gameObject.SetActive(true);
                }

                for (int i = 0; i < actions.Count; i++)
                {
                    var actionData = actions[i];
                    if (actionData == null) continue;
                    var actionPrefab = _system.ResolveActionButtonPrefab();
                    if (actionPrefab == null) continue;
                    var parent = _actionsRoot != null ? _actionsRoot : _contentRoot;
                    var actionInstance = Instantiate(actionPrefab, parent);
                    actionInstance.Initialize(actionData, _handle, _request.PlayerContext);
                    _spawnedActions.Add(actionInstance);
                }

                if (_actionsLayout != null)
                {
                    _actionsLayout.enabled = true;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_actionsRoot ?? _contentRoot);
                }
            }
            else if (_actionsRoot != null)
            {
                _actionsRoot.gameObject.SetActive(false);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        }

        public void ReplaceContent(IReadOnlyList<TooltipContentData> content, IReadOnlyList<TooltipActionData> actions)
        {
            _request.Content = content;
            _request.Actions = actions;
            BuildContent();
            UpdatePosition(true);
        }

        public void UpdateOffset(Vector2 offset)
        {
            _request.Offset = offset;
            _currentOffset = _system.DefaultOffset + offset;
            UpdatePosition(true);
        }

        public void UpdateAnchor(RectTransform anchor, bool followTarget)
        {
            _request.Anchor = anchor;
            _request.ScreenPosition = null;
            _request.WorldPosition = null;
            _isFollowing = followTarget;
            UpdatePosition(true);
        }

        public void UpdateWorldPosition(Vector3 worldPosition, bool followTarget)
        {
            _request.WorldPosition = worldPosition;
            _worldPosition = worldPosition;
            _request.Anchor = null;
            _request.ScreenPosition = null;
            _isFollowing = followTarget;
            UpdatePosition(true);
        }

        public void SetVisibility(bool visible)
        {
            _isVisible = visible;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }
        }

        private void ClearContent()
        {
            for (int i = 0; i < _spawnedContent.Count; i++)
            {
                if (_spawnedContent[i] != null)
                {
                    _spawnedContent[i].Unbind();
                    Destroy(_spawnedContent[i].gameObject);
                }
            }
            _spawnedContent.Clear();
        }

        private void ClearActions()
        {
            for (int i = 0; i < _spawnedActions.Count; i++)
            {
                if (_spawnedActions[i] != null)
                {
                    _spawnedActions[i].Release();
                    Destroy(_spawnedActions[i].gameObject);
                }
            }
            _spawnedActions.Clear();
        }

        private void LateUpdate()
        {
            if (_system == null || !_isVisible)
            {
                return;
            }

            if (!_isFollowing)
            {
                if (_request.ScreenPosition.HasValue)
                {
                    return;
                }

                if (_worldPosition == null && _request.Anchor == null)
                {
                    return;
                }
            }

            UpdatePosition();
        }

        private void UpdatePosition(bool forceRebuildLayout = false)
        {
            if (_system == null)
            {
                return;
            }

            var canvas = _system.GetCanvasOrThrow();
            var layer = _system.GetTooltipLayerOrThrow();
            var camera = _request.PlayerContext.UICamera != null ? _request.PlayerContext.UICamera : _system.GetUICamera();

            if (forceRebuildLayout)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
            }

            Vector2 screenPoint = ResolveScreenPoint(camera);
            screenPoint += _currentOffset;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, camera, out var localPoint);
            _root.anchoredPosition = localPoint;

            if (ShouldClamp())
            {
                ClampToCanvas(layer);
            }
        }

        private bool ShouldClamp()
        {
            if (_request.ClampToViewport.HasValue)
            {
                return _request.ClampToViewport.Value;
            }

            return _system.ClampToViewport;
        }

        private Vector2 ResolveScreenPoint(Camera camera)
        {
            if (_request.ScreenPosition.HasValue)
            {
                return _request.ScreenPosition.Value;
            }

            if (_worldPosition.HasValue)
            {
                return RectTransformUtility.WorldToScreenPoint(camera, _worldPosition.Value);
            }

            if (_request.Anchor != null)
            {
                return RectTransformUtility.WorldToScreenPoint(camera, _request.Anchor.position);
            }

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
#endif
            return Input.mousePosition;
        }

        private void ClampToCanvas(RectTransform layer)
        {
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(layer, _root);
            var container = layer.rect;
            var anchoredPosition = _root.anchoredPosition;

            float deltaX = 0f;
            float deltaY = 0f;

            if (bounds.min.x < container.xMin)
            {
                deltaX = container.xMin - bounds.min.x;
            }
            else if (bounds.max.x > container.xMax)
            {
                deltaX = container.xMax - bounds.max.x;
            }

            if (bounds.min.y < container.yMin)
            {
                deltaY = container.yMin - bounds.min.y;
            }
            else if (bounds.max.y > container.yMax)
            {
                deltaY = container.yMax - bounds.max.y;
            }

            if (deltaX != 0f || deltaY != 0f)
            {
                anchoredPosition += new Vector2(deltaX, deltaY);
                _root.anchoredPosition = anchoredPosition;
            }
        }
    }
}
