using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Entry point for showing and managing tooltips. Drop this on a persistent UI object and assign the references once.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TooltipSystemRoot : MonoBehaviour
    {
        private static TooltipSystemRoot _instance;

        [Header("Runtime References")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _tooltipLayer;
        [SerializeField] private TooltipView _tooltipPrefab;
        [SerializeField] private TooltipActionButtonView _actionButtonPrefab;

        [Header("Content Registry")]
        [SerializeField] private List<TooltipContentViewBase> _contentPrefabs = new();

        [Header("Behaviour")]
        [SerializeField, Tooltip("Clamp tooltip rect inside the canvas viewport whenever possible.")]
        private bool _clampToViewport = true;
        [SerializeField, Tooltip("Optional default offset that is applied on top of the request offset.")]
        private Vector2 _defaultScreenOffset = new Vector2(12f, -12f);

        private readonly List<TooltipView> _activeViews = new();
        private readonly Queue<TooltipView> _pool = new();
        private readonly Dictionary<Type, TooltipContentViewBase> _registry = new();
        private Camera _cachedWorldCamera;

        public static TooltipSystemRoot Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TooltipSystemRoot>();
                    if (_instance == null)
                    {
                        Debug.LogError("TooltipSystemRoot was not found in the scene. Please add one.");
                    }
                }
                return _instance;
            }
        }

        public Canvas Canvas => _canvas;
        public RectTransform TooltipLayer => _tooltipLayer;
        public bool ClampToViewport => _clampToViewport;
        public TooltipActionButtonView ActionButtonPrefab => _actionButtonPrefab;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Multiple TooltipSystemRoot instances detected. Destroying the newest instance.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            EnsureCanvasSetup();
            WarmUpRegistry();
        }

        private void EnsureCanvasSetup()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                if (_canvas == null)
                {
                    _canvas = GetComponent<Canvas>();
                }
            }

            if (_tooltipLayer == null && _canvas != null)
            {
                _tooltipLayer = _canvas.transform as RectTransform;
            }

            if (_canvas != null)
            {
                _cachedWorldCamera = _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null;
            }
        }

        private void WarmUpRegistry()
        {
            _registry.Clear();
            foreach (var prefab in _contentPrefabs)
            {
                if (prefab == null) continue;
                var type = prefab.SupportedDataType;
                if (type == null)
                {
                    Debug.LogWarning($"Tooltip prefab '{prefab.name}' does not declare a supported data type.");
                    continue;
                }

                if (_registry.ContainsKey(type))
                {
                    Debug.LogWarning($"Tooltip content type '{type.Name}' is registered multiple times. The first entry wins.");
                    continue;
                }

                _registry.Add(type, prefab);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Registers or overrides a tooltip content view prefab for the provided data type at runtime.
        /// </summary>
        public void RegisterContentPrefab(Type dataType, TooltipContentViewBase viewPrefab)
        {
            if (dataType == null) throw new ArgumentNullException(nameof(dataType));
            if (viewPrefab == null) throw new ArgumentNullException(nameof(viewPrefab));

            _registry[dataType] = viewPrefab;
            if (!_contentPrefabs.Contains(viewPrefab))
            {
                _contentPrefabs.Add(viewPrefab);
            }
        }

        public TooltipHandle ShowTooltip(in TooltipRequest request)
        {
            EnsureCanvasSetup();
            WarmUpRegistryIfNeeded();

            var view = SpawnView();
            var handle = new TooltipHandle(this, view);
            view.Initialize(request, handle);
            _activeViews.Add(view);
            return handle;
        }

        public void DismissTooltip(TooltipHandle handle)
        {
            if (!handle.IsValid) return;
            var view = handle.View;
            if (view == null) return;

            if (_activeViews.Remove(view))
            {
                view.Release();
                ReturnToPool(view);
            }
        }

        internal TooltipContentViewBase ResolveViewPrefab(Type dataType)
        {
            if (dataType == null)
            {
                return null;
            }

            if (_registry.TryGetValue(dataType, out var prefab))
            {
                return prefab;
            }

            foreach (var kvp in _registry)
            {
                if (kvp.Key.IsAssignableFrom(dataType))
                {
                    _registry[dataType] = kvp.Value;
                    return kvp.Value;
                }
            }

            Debug.LogWarning($"No tooltip view prefab registered for data type '{dataType.Name}'.");
            return null;
        }

        internal TooltipActionButtonView ResolveActionButtonPrefab()
        {
            if (_actionButtonPrefab == null)
            {
                Debug.LogWarning("Tooltip action button prefab is not assigned.");
            }
            return _actionButtonPrefab;
        }

        internal Canvas GetCanvasOrThrow()
        {
            Assert.IsNotNull(_canvas, "Tooltip canvas is not configured.");
            return _canvas;
        }

        internal RectTransform GetTooltipLayerOrThrow()
        {
            Assert.IsNotNull(_tooltipLayer, "Tooltip layer rect is not configured.");
            return _tooltipLayer;
        }

        internal Camera GetUICamera()
        {
            if (_canvas == null) return null;
            return _canvas.renderMode == RenderMode.ScreenSpaceCamera ? _canvas.worldCamera : null;
        }

        internal Vector2 DefaultOffset => _defaultScreenOffset;

        private TooltipView SpawnView()
        {
            TooltipView view = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(_tooltipPrefab, _tooltipLayer);
            view.gameObject.SetActive(true);
            return view;
        }

        private void ReturnToPool(TooltipView view)
        {
            if (view == null) return;
            view.gameObject.SetActive(false);
            _pool.Enqueue(view);
        }

        private void WarmUpRegistryIfNeeded()
        {
            if (_registry.Count == 0 && _contentPrefabs.Count > 0)
            {
                WarmUpRegistry();
            }
        }
    }
}
