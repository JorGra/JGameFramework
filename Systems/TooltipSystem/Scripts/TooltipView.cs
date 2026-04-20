using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private Vector2 _basePivot;
        private bool _isVisible = true;
        private Vector3? _worldPosition;
        private bool _isFollowing;
        private bool _blocksRaycasts;
        private bool _isSticky;
        private GameObject _previousSelection;
        private bool _closedByPointer;
        private bool _hijackedSelection;
        private EventSystem _hijackedEventSystem;

        public TooltipPlayerContext PlayerContext => _request.PlayerContext;
        public object Tag { get; private set; }
        internal bool BlocksRaycasts => _blocksRaycasts;
        internal bool IsSticky => _isSticky;

        internal void Initialize(TooltipRequest request, TooltipHandle handle)
        {
            _system = TooltipSystemRoot.Instance;
            _request = request;
            _handle = handle;
            _worldPosition = request.WorldPosition;
            _isFollowing = request.FollowTarget;
            _blocksRaycasts = request.BlocksRaycasts;
            _isSticky = request.Sticky;
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
            _basePivot = _root.pivot;

            _previousSelection = null;
            _closedByPointer = false;
            _hijackedSelection = false;
            _hijackedEventSystem = null;

            SetupSorting(request.SortingOffset);
            BuildContent();
            UpdatePosition(true);
            SetVisibility(true);
        }

        internal void Release()
        {
            RestorePreviousSelection();
            ClearContent();
            ClearActions();
            _system = null;
            _handle = default;
            _request = default;
            Tag = null;
            _worldPosition = null;
            _isFollowing = false;
            _blocksRaycasts = false;
            _isSticky = false;
            _currentOffset = Vector2.zero;
            _basePivot = Vector2.zero;
            _isVisible = false;
            ApplyCanvasGroupState(false);
        }

        private void RestorePreviousSelection()
        {
            // Only tooltips that took focus (context menus with action buttons) need
            // to restore anything. Hover tooltips leave EventSystem selection alone.
            if (!_hijackedSelection)
            {
                _previousSelection = null;
                return;
            }

            var es = _hijackedEventSystem != null ? _hijackedEventSystem : ResolveEventSystem();
            GameObject target = null;
            if (!_closedByPointer && _previousSelection != null && _previousSelection.activeInHierarchy)
            {
                target = _previousSelection;
            }
            _previousSelection = null;
            _hijackedSelection = false;
            _hijackedEventSystem = null;

            if (es == null) return;

            // Defer through the system root so the selection change happens on a
            // fresh frame, avoiding Unity's "already selecting an object" warning
            // when Release runs from inside an OnDeselect callback.
            if (_system != null)
            {
                _system.ScheduleSelect(es, target);
            }
            else
            {
                es.SetSelectedGameObject(target);
            }
        }

        internal void NotifyActionClickedByPointer()
        {
            _closedByPointer = true;
        }

        // Prefer the EventSystem that is actively processing the current event
        // (EventSystem.current during a click / submit handler). In multiplayer
        // setups that instance is the player's MES — using any other EventSystem
        // would target a different player's selection state and have no visible
        // effect. Fall back to values baked into the tooltip request only if
        // there is no active EventSystem.
        private EventSystem ResolveEventSystem()
        {
            var current = EventSystem.current;
#if ENABLE_INPUT_SYSTEM
            if (current is UnityEngine.InputSystem.UI.MultiplayerEventSystem) return current;
#endif
            var ctx = _request.PlayerContext;
            if (ctx.MultiplayerEventSystem != null) return ctx.MultiplayerEventSystem;
            if (ctx.EventSystem != null) return ctx.EventSystem;
            return current;
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
            bool hasActions = actions != null && actions.Count > 0;
            bool shouldRenderActions = _request.IsContextMenu && hasActions;

            if (!_request.IsContextMenu && hasActions)
            {
                Debug.LogWarning("Received tooltip actions for a non-context-menu presentation. Actions will be ignored.", this);
                shouldRenderActions = false;
            }

            if (shouldRenderActions)
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

                if (_spawnedActions.Count > 0)
                {
                    // Capture hijack state up-front so restore on close always has
                    // a target, independent of whether the auto-select below
                    // succeeds in time.
                    var hijackEs = ResolveEventSystem();
                    if (hijackEs != null)
                    {
                        _previousSelection = hijackEs.currentSelectedGameObject;
                        _hijackedSelection = true;
                        _hijackedEventSystem = hijackEs;
                    }

                    ConfigureActionButtonNavigation();
                    SelectBottomActionButton();
                }
            }
            else if (_actionsRoot != null)
            {
                _actionsRoot.gameObject.SetActive(false);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
        }

        // Wires explicit vertical navigation between spawned action buttons so controller
        // input cannot escape the tooltip while it is open. Buttons outside the list
        // (e.g. the shop reroll button) remain unreachable until an action is chosen.
        private void ConfigureActionButtonNavigation()
        {
            for (int i = 0; i < _spawnedActions.Count; i++)
            {
                var btn = _spawnedActions[i];
                if (btn == null) continue;

                var nav = btn.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = i > 0 ? _spawnedActions[i - 1] : null;
                nav.selectOnDown = i < _spawnedActions.Count - 1 ? _spawnedActions[i + 1] : null;
                nav.selectOnLeft = null;
                nav.selectOnRight = null;
                btn.navigation = nav;
            }
        }

        // Focuses the bottommost spawned action button. Layout was rebuilt synchronously
        // above so world positions are valid. Only runs when actions exist, so hover
        // tooltips never touch EventSystem selection.
        private void SelectBottomActionButton()
        {
            var eventSystem = _hijackedEventSystem != null ? _hijackedEventSystem : ResolveEventSystem();
            if (eventSystem == null) return;

            TooltipActionButtonView bottom = null;
            float minY = float.MaxValue;
            for (int i = 0; i < _spawnedActions.Count; i++)
            {
                var btn = _spawnedActions[i];
                // Use the Button.interactable flag directly. Button.IsInteractable()
                // also checks CanvasGroup which is not yet enabled at BuildContent
                // time (SetVisibility runs after us), so it would filter everything.
                if (btn == null || !btn.interactable) continue;
                float y = btn.transform.position.y;
                if (y < minY)
                {
                    minY = y;
                    bottom = btn;
                }
            }

            if (bottom == null) return;

            if (_system != null)
            {
                _system.ScheduleSelectPersistent(eventSystem, bottom.gameObject, 10);
            }
            else
            {
                eventSystem.SetSelectedGameObject(bottom.gameObject);
            }
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
            ApplyCanvasGroupState(visible);
        }

        private void ApplyCanvasGroupState(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            bool interactive = visible && _blocksRaycasts;
            _canvasGroup.blocksRaycasts = interactive;
            _canvasGroup.interactable = interactive;
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
            if (_system == null || _root == null)
            {
                return;
            }

            var layer = (_root.parent as RectTransform) ?? _system.GetTooltipLayerOrThrow();
            var canvas = layer != null ? layer.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                canvas = _system.GetCanvasOrThrow();
                layer = _system.GetTooltipLayerOrThrow();
            }

            var camera = ResolveCameraForCanvas(canvas);

            if (forceRebuildLayout)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
            }

            var anchorScreenPoint = ResolveScreenPoint(camera);
            var resolvedOffset = _currentOffset;
            var resolvedPivot = _basePivot;

            if (!TryPlaceTooltip(layer, camera, anchorScreenPoint, resolvedOffset, resolvedPivot))
            {
                return;
            }

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(layer, _root);

            if (TryFlipHorizontal(anchorScreenPoint, layer, camera, ref resolvedOffset, ref resolvedPivot, bounds))
            {
                bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(layer, _root);
            }

            if (TryFlipVertical(anchorScreenPoint, layer, camera, ref resolvedOffset, ref resolvedPivot, bounds))
            {
                bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(layer, _root);
            }

            if (ShouldClamp())
            {
                ClampToCanvas(layer);
            }
        }

        private bool TryPlaceTooltip(RectTransform layer, Camera camera, Vector2 anchorScreenPoint, Vector2 offset, Vector2 pivot)
        {
            _root.pivot = pivot;

            var screenPoint = anchorScreenPoint + offset;

            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(layer, screenPoint, camera, out var worldPoint))
            {
                return false;
            }

            _root.position = worldPoint;
            return true;
        }

        private bool TryFlipHorizontal(Vector2 anchorScreenPoint, RectTransform layer, Camera camera, ref Vector2 offset, ref Vector2 pivot, Bounds currentBounds)
        {
            var container = layer.rect;
            bool overflowRight = currentBounds.max.x > container.xMax;
            bool overflowLeft = currentBounds.min.x < container.xMin;

            if ((overflowLeft && overflowRight) || (!overflowLeft && !overflowRight))
            {
                return false;
            }

            float orientation = Mathf.Approximately(offset.x, 0f) ? 0.5f - pivot.x : offset.x;

            if (Mathf.Approximately(orientation, 0f))
            {
                return false;
            }

            if (orientation > 0f && !overflowRight)
            {
                return false;
            }

            if (orientation < 0f && !overflowLeft)
            {
                return false;
            }

            var candidateOffset = offset;
            candidateOffset.x = FlipOffset(candidateOffset.x);

            var candidatePivot = pivot;
            candidatePivot.x = 1f - candidatePivot.x;

            if (!TryPlaceTooltip(layer, camera, anchorScreenPoint, candidateOffset, candidatePivot))
            {
                TryPlaceTooltip(layer, camera, anchorScreenPoint, offset, pivot);
                return false;
            }

            offset = candidateOffset;
            pivot = candidatePivot;
            return true;
        }

        private bool TryFlipVertical(Vector2 anchorScreenPoint, RectTransform layer, Camera camera, ref Vector2 offset, ref Vector2 pivot, Bounds currentBounds)
        {
            var container = layer.rect;
            bool overflowTop = currentBounds.max.y > container.yMax;
            bool overflowBottom = currentBounds.min.y < container.yMin;

            if ((overflowTop && overflowBottom) || (!overflowTop && !overflowBottom))
            {
                return false;
            }

            float orientation = Mathf.Approximately(offset.y, 0f) ? 0.5f - pivot.y : offset.y;

            if (Mathf.Approximately(orientation, 0f))
            {
                return false;
            }

            if (orientation > 0f && !overflowTop)
            {
                return false;
            }

            if (orientation < 0f && !overflowBottom)
            {
                return false;
            }

            var candidateOffset = offset;
            candidateOffset.y = FlipOffset(candidateOffset.y);

            var candidatePivot = pivot;
            candidatePivot.y = 1f - candidatePivot.y;

            if (!TryPlaceTooltip(layer, camera, anchorScreenPoint, candidateOffset, candidatePivot))
            {
                TryPlaceTooltip(layer, camera, anchorScreenPoint, offset, pivot);
                return false;
            }

            offset = candidateOffset;
            pivot = candidatePivot;
            return true;
        }

        private static float FlipOffset(float value)
        {
            return Mathf.Approximately(value, 0f) ? value : -value;
        }

        private Camera ResolveCameraForCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                if (_request.PlayerContext.UICamera != null)
                {
                    return _request.PlayerContext.UICamera;
                }

                return _system != null ? _system.GetUICamera() : null;
            }

            switch (canvas.renderMode)
            {
                case RenderMode.ScreenSpaceOverlay:
                    return null;
                case RenderMode.ScreenSpaceCamera:
                case RenderMode.WorldSpace:
                    if (canvas.worldCamera != null)
                    {
                        return canvas.worldCamera;
                    }
                    break;
            }

            if (_request.PlayerContext.UICamera != null)
            {
                return _request.PlayerContext.UICamera;
            }

            return _system.GetUICamera();
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





