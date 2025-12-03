using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;


namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Per-player facade that coordinates tooltip and context menu presentations.
    /// Attach this to the root of a player's UI and wire up the relevant input references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerTooltipController : MonoBehaviour
    {
        [Header("Player Context")]
        [SerializeField] private EventSystem _eventSystem;
        [SerializeField] private Camera _uiCamera;
        [SerializeField] private int _playerIndex = -1;
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private MultiplayerEventSystem _multiplayerEventSystem;
        [SerializeField] private InputSystemUIInputModule _inputModule;

        [SerializeField] private RectTransform _tooltipLayerOverride;

        [Header("Behaviour")]
        [SerializeField, Tooltip("If true, all hover tooltips owned by this controller close when the object disables.")]
        private bool _closeTooltipsOnDisable = true;
        [SerializeField, Tooltip("If true, all context menus owned by this controller close when the object disables.")]
        private bool _closeMenusOnDisable = true;
        [SerializeField, Tooltip("When enabled, opening a context menu closes any other menu owned by this controller.")]
        private bool _exclusiveContextMenus = true;

        private readonly Dictionary<object, TooltipHandle> _tooltips = new();
        private readonly Dictionary<object, TooltipHandle> _contextMenus = new();

        private void OnDisable()
        {
            if (_closeTooltipsOnDisable)
            {
                CloseAllTooltips();
            }

            if (_closeMenusOnDisable)
            {
                CloseAllContextMenus();
            }
        }

        #region Public API

        public TooltipHandle ShowTooltip(
            object owner,
            RectTransform anchor,
            bool followTarget,
            Action<TooltipBuilder> configure,
            BaseEventData eventData = null,
            TooltipPlayerContext? contextOverride = null)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var context = contextOverride ?? ResolvePlayerContext(eventData as PointerEventData);
            CloseTooltip(owner);

            var builder = new TooltipBuilder()
                .WithPlayerContext(context)
                .WithTag(owner);

            if (_tooltipLayerOverride != null)
            {
                builder.WithLayer(_tooltipLayerOverride);
            }

            if (anchor != null)
            {
                builder.WithAnchor(anchor, followTarget);
            }

            configure(builder);

            var handle = builder.Show();
            if (handle.IsValid)
            {
                _tooltips[owner] = handle;
            }
            return handle;
        }

        public TooltipHandle ShowTooltip(
            object owner,
            RectTransform anchor,
            Action<TooltipBuilder> configure,
            BaseEventData eventData = null,
            TooltipPlayerContext? contextOverride = null)
        {
            return ShowTooltip(owner, anchor, followTarget: true, configure: configure, eventData: eventData, contextOverride: contextOverride);
        }

        public TooltipHandle ShowContextMenu(
            object owner,
            RectTransform anchor,
            Action<ContextMenuBuilder> configure,
            BaseEventData eventData = null,
            TooltipPlayerContext? contextOverride = null,
            bool followTarget = false)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var context = contextOverride ?? ResolvePlayerContext(eventData as PointerEventData);

            if (_exclusiveContextMenus)
            {
                CloseAllContextMenus();
            }
            else
            {
                CloseContextMenu(owner);
            }

            var builder = new ContextMenuBuilder()
                .WithPlayerContext(context)
                .WithTag(owner);

            if (_tooltipLayerOverride != null)
            {
                builder.WithLayer(_tooltipLayerOverride);
            }

            if (anchor != null)
            {
                builder.WithAnchor(anchor, followTarget);
            }

            configure(builder);

            var handle = builder.Show();
            if (handle.IsValid)
            {
                _contextMenus[owner] = handle;
            }
            return handle;
        }

        public bool ToggleContextMenu(
            object owner,
            RectTransform anchor,
            Action<ContextMenuBuilder> configure,
            BaseEventData eventData = null,
            TooltipPlayerContext? contextOverride = null,
            bool followTarget = false)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            if (HasContextMenu(owner))
            {
                CloseContextMenu(owner);
                return true;
            }

            var handle = ShowContextMenu(owner, anchor, configure, eventData, contextOverride, followTarget);
            return handle.IsValid;
        }

        public void CloseTooltip(object owner)
        {
            if (owner == null)
            {
                return;
            }

            if (_tooltips.TryGetValue(owner, out var handle))
            {
                CloseHandle(handle);
                _tooltips.Remove(owner);
            }
        }

        public void CloseContextMenu(object owner)
        {
            if (owner == null)
            {
                return;
            }

            if (_contextMenus.TryGetValue(owner, out var handle))
            {
                CloseHandle(handle);
                _contextMenus.Remove(owner);
            }
        }

        public void CloseAllTooltips()
        {
            foreach (var kvp in _tooltips)
            {
                CloseHandle(kvp.Value);
            }
            _tooltips.Clear();
        }

        public void CloseAllContextMenus()
        {
            foreach (var kvp in _contextMenus)
            {
                CloseHandle(kvp.Value);
            }
            _contextMenus.Clear();
        }

        public bool HasTooltip(object owner)
        {
            return TryPruneAndCheck(_tooltips, owner);
        }

        public bool HasContextMenu(object owner)
        {
            return TryPruneAndCheck(_contextMenus, owner);
        }

        public TooltipPlayerContext ResolvePlayerContext()
        {
            return ResolvePlayerContext(pointerEvent: null);
        }

        public void SetTooltipLayer(RectTransform layer)
        {
            _tooltipLayerOverride = layer;
        }

        public void SetPlayerContext(EventSystem eventSystem, Camera uiCamera, int playerIndex = -1)
        {
            _eventSystem = eventSystem;
            _uiCamera = uiCamera;
            _playerIndex = playerIndex;

            if (eventSystem is MultiplayerEventSystem multiplayerEventSystem)
            {
                _multiplayerEventSystem = multiplayerEventSystem;
            }

            if (eventSystem != null)
            {
                var module = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (module != null)
                {
                    _inputModule = module;
                }
            }

        }

        public void SetPlayerContext(PlayerInput playerInput, MultiplayerEventSystem multiplayerEventSystem = null, InputSystemUIInputModule inputModule = null)
        {
            _playerInput = playerInput;

            _multiplayerEventSystem = multiplayerEventSystem != null
                ? multiplayerEventSystem
                : playerInput != null ? playerInput.GetComponentInChildren<MultiplayerEventSystem>() : null;

            if (_multiplayerEventSystem != null)
            {
                _eventSystem = _multiplayerEventSystem;
            }

            if (inputModule != null)
            {
                _inputModule = inputModule;
            }
            else
            {
                if (_multiplayerEventSystem != null)
                {
                    var moduleFromEvent = _multiplayerEventSystem.GetComponent<InputSystemUIInputModule>();
                    if (moduleFromEvent != null)
                    {
                        _inputModule = moduleFromEvent;
                    }
                }

                if (_inputModule == null && playerInput != null)
                {
                    _inputModule = playerInput.GetComponentInChildren<InputSystemUIInputModule>();
                }
            }

            if (playerInput != null)
            {
                if (playerInput.camera != null)
                {
                    _uiCamera = playerInput.camera;
                }

                _playerIndex = playerInput.playerIndex;
            }
        }


        #endregion

        #region Helpers

        private bool TryPruneAndCheck(Dictionary<object, TooltipHandle> map, object owner)
        {
            if (owner == null)
            {
                return false;
            }

            if (!map.TryGetValue(owner, out var handle))
            {
                return false;
            }

            if (IsHandleAlive(handle))
            {
                return true;
            }

            map.Remove(owner);
            return false;
        }

        private static bool IsHandleAlive(TooltipHandle handle)
        {
            return handle.IsValid && handle.View != null && handle.View.gameObject.activeInHierarchy;
        }

        private static void CloseHandle(TooltipHandle handle)
        {
            if (handle.IsValid)
            {
                handle.Close();
            }
        }

                private TooltipPlayerContext ResolvePlayerContext(PointerEventData pointerEvent)
        {
            var camera = ResolveCamera(pointerEvent);

            if (_playerInput != null)
            {
                var context = TooltipPlayerContextUtility.FromPlayerInput(_playerInput, camera);

                if (_inputModule != null && context.InputModule == null)
                {
                    context.InputModule = _inputModule;
                }

                if (_multiplayerEventSystem != null)
                {
                    context.MultiplayerEventSystem = _multiplayerEventSystem;
                    if (context.EventSystem == null)
                    {
                        context.EventSystem = _multiplayerEventSystem;
                    }
                }

                if (context.EventSystem == null)
                {
                    context.EventSystem = _eventSystem != null ? _eventSystem : ResolveEventSystem(pointerEvent);
                }

                return context;
            }

            if (_multiplayerEventSystem != null)
            {
                var context = TooltipPlayerContextUtility.FromEventSystem(
                    _multiplayerEventSystem,
                    camera,
                    ResolvePlayerIndex());

                if (_inputModule != null && context.InputModule == null)
                {
                    context.InputModule = _inputModule;
                }

                return context;
            }

            var eventSystem = ResolveEventSystem(pointerEvent);
            return TooltipPlayerContextUtility.FromEventSystem(eventSystem, camera, ResolvePlayerIndex());
        }

        private int ResolvePlayerIndex()
        {
            if (_playerIndex >= 0)
            {
                return _playerIndex;
            }

            if (_playerInput != null)
            {
                return _playerInput.playerIndex;
            }

            if (_multiplayerEventSystem != null && _multiplayerEventSystem.playerRoot != null)
            {
                var input = _multiplayerEventSystem.playerRoot.GetComponent<PlayerInput>();
                if (input != null)
                {
                    return input.playerIndex;
                }
            }


            return -1;
        }

        private EventSystem ResolveEventSystem(PointerEventData pointerEvent)
        {
            if (_multiplayerEventSystem != null)
            {
                return _multiplayerEventSystem;
            }

            if (_eventSystem != null)
            {
                return _eventSystem;
            }

            return EventSystem.current;
        }

        private Camera ResolveCamera(PointerEventData pointerEvent)
        {
            if (pointerEvent != null)
            {
                if (pointerEvent.enterEventCamera != null)
                {
                    return pointerEvent.enterEventCamera;
                }

                if (pointerEvent.pressEventCamera != null)
                {
                    return pointerEvent.pressEventCamera;
                }
            }

            if (_uiCamera != null)
            {
                return _uiCamera;
            }

            return Camera.main;
        }

        #endregion
    }
}
