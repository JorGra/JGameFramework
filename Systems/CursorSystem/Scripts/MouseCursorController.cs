using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.CursorSystem
{
    /// <summary>
    /// Central mouse cursor manager that responds to <see cref="CursorChangeRequestEvent"/>s.
    /// Keeps the logic project-agnostic while allowing different preset sets (e.g. menu vs gameplay).
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [AddComponentMenu("JGameFramework/Cursor System/Mouse Cursor Controller")]
    [DisallowMultipleComponent]
    public sealed class MouseCursorController : MonoBehaviour
    {
        [Header("Preset Source")]
        [SerializeField] CursorPresetLibrary presetLibrary;
        [SerializeField] string defaultCursorSetId = "Default";
        [SerializeField] string defaultPresetId = "Default";
        [SerializeField] bool applyDefaultOnAwake = true;
        [SerializeField] bool dontDestroyOnLoad = true;

        [Header("Platform Tweaks")]
        [Tooltip("Linux only: cursor textures larger than this (pixels) get downscaled to avoid huge hardware cursors.")]
        [SerializeField, Min(8)] int linuxMaxCursorSize = 64;
        [Tooltip("Linux only: target size (pixels) to scale cursors to; 0 = use max size only.")]
        [SerializeField, Min(0)] int linuxTargetCursorSize = 64;
        [Tooltip("Linux only: forces software cursor to bypass OS hardware cursor issues (dual cursors, wrong scaling).")]
        [SerializeField] bool linuxForceSoftwareCursor = true;

        [Header("Debug")]
        [SerializeField] bool logWarnings = true;
        [SerializeField] bool logInfo = false;

        readonly Dictionary<string, CursorSetDefinition> setLookup =
            new Dictionary<string, CursorSetDefinition>(StringComparer.OrdinalIgnoreCase);

        CursorSetDefinition activeSet;
        CursorPreset activePreset;
        string activeSetId;
        string activePresetId;
        Texture2D cachedScaledTexture;

        EventSubscription<CursorChangeRequestEvent> changeSubscription;

        public string ActiveSetId => activeSetId;
        public string ActivePresetId => activePresetId;

        static MouseCursorController persistentInstance;

        void Awake()
        {
            if (dontDestroyOnLoad)
            {
                if (persistentInstance != null && persistentInstance != this)
                {
                    // Another instance already survives scene loads; drop only this component
                    // so other behaviours on the same GameObject (e.g., scene preset selectors) keep running.
                    Destroy(this);
                    return;
                }

                persistentInstance = this;
                DontDestroyOnLoad(gameObject);
            }

            BuildLookup();

            changeSubscription = EventBus<CursorChangeRequestEvent>.Subscribe(HandleCursorChangeRequest, this);

            if (applyDefaultOnAwake)
            {
                ApplyPreset(defaultPresetId, defaultCursorSetId, fallbackToDefault: true, forceRefresh: true, logMissing: false);
            }
        }

        void OnDestroy()
        {
            if (persistentInstance == this)
                persistentInstance = null;

            changeSubscription?.Dispose();
            changeSubscription = null;

            if (cachedScaledTexture != null)
            {
                Destroy(cachedScaledTexture);
                cachedScaledTexture = null;
            }
        }

        void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            BuildLookup();
            if (activeSet == null)
            {
                ApplyPreset(defaultPresetId, defaultCursorSetId, fallbackToDefault: true, forceRefresh: true, logMissing: false);
            }
        }

        void BuildLookup()
        {
            setLookup.Clear();
            if (presetLibrary == null)
                return;

            foreach (var set in presetLibrary.CursorSets)
            {
                if (set == null)
                    continue;

                var id = set.SetId;
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                setLookup[id] = set;
            }
        }

        void HandleCursorChangeRequest(CursorChangeRequestEvent request)
        {
            LogInfo($"Request: set='{request.CursorSetId ?? "<null>"}' preset='{request.PresetId ?? "<null>"}' force={request.ForceRefresh}");

            var successfullyApplied = ApplyPreset(
                request.PresetId,
                request.CursorSetId,
                request.AllowFallbackToDefaultPreset,
                request.ForceRefresh,
                logMissing: true);

            if (!successfullyApplied && logWarnings)
            {
                Debug.LogWarning(
                    $"[MouseCursorController] Could not apply preset '{request.PresetId ?? "<default>"}' " +
                    $"from set '{request.CursorSetId ?? ActiveSetId ?? defaultCursorSetId}'.");
            }

            if (request.LockMode.HasValue)
                Cursor.lockState = request.LockMode.Value;

            if (request.CursorVisibility.HasValue)
                Cursor.visible = request.CursorVisibility.Value;
            else if (activePreset != null && activePreset.OverrideCursorVisibility)
                Cursor.visible = activePreset.CursorVisible;
        }

        bool ApplyPreset(
            string presetId,
            string setId,
            bool fallbackToDefault,
            bool forceRefresh,
            bool logMissing)
        {
            var targetSet = ResolveSet(setId, logMissing);
            if (targetSet == null)
                return false;

            var targetPreset = ResolvePreset(targetSet, presetId, fallbackToDefault, logMissing);
            if (targetPreset == null)
                return false;

            if (!forceRefresh && ReferenceEquals(targetSet, activeSet) && ReferenceEquals(targetPreset, activePreset))
                return true;

            if (!targetPreset.HasTexture)
            {
                if (logWarnings && logMissing)
                {
                    Debug.LogWarning(
                        $"[MouseCursorController] Preset '{targetPreset.PresetId}' in set '{targetSet.SetId}' is missing a texture.");
                }
                return false;
            }

            if (!TryGetPlatformCursorData(targetPreset, out var texture, out var hotSpot, out var mode))
                return false;

            Cursor.SetCursor(texture, hotSpot, mode);
            if (targetPreset.OverrideCursorVisibility)
                Cursor.visible = targetPreset.CursorVisible;

            var previousSetId = activeSetId;
            activeSet = targetSet;
            activePreset = targetPreset;
            activeSetId = targetSet.SetId;
            activePresetId = !string.IsNullOrWhiteSpace(targetPreset.PresetId)
                ? targetPreset.PresetId
                : (presetId ?? defaultPresetId);

            LogInfo($"Applied cursor set='{activeSetId}', preset='{activePresetId}', tex={texture.width}x{texture.height}, hotspot={hotSpot}, mode={mode}");

            if (!string.Equals(previousSetId, activeSetId, StringComparison.Ordinal))
            {
                EventBus<CursorSetChangedEvent>.Raise(new CursorSetChangedEvent(activeSetId));
            }

            EventBus<CursorPresetChangedEvent>.Raise(
                new CursorPresetChangedEvent(activeSetId, activePresetId, targetPreset));
            return true;
        }

        CursorSetDefinition ResolveSet(string requestedSetId, bool logMissing)
        {
            if (string.IsNullOrWhiteSpace(requestedSetId))
            {
                if (activeSet != null)
                    return activeSet;

                requestedSetId = defaultCursorSetId;
            }

            if (!string.IsNullOrWhiteSpace(requestedSetId) &&
                setLookup.TryGetValue(requestedSetId, out var matching))
            {
                return matching;
            }

            if (presetLibrary != null)
            {
                var fallback = presetLibrary.GetFirstValidSet();
                if (fallback != null)
                {
                    if (logWarnings && logMissing && !string.IsNullOrWhiteSpace(requestedSetId))
                    {
                        Debug.LogWarning(
                            $"[MouseCursorController] Cursor set '{requestedSetId}' not found. Falling back to '{fallback.SetId}'.");
                    }

                    return fallback;
                }
            }

            if (logWarnings && logMissing)
            {
                Debug.LogWarning("[MouseCursorController] No cursor sets available.");
            }
            return null;
        }

        CursorPreset ResolvePreset(
            CursorSetDefinition set,
            string presetId,
            bool fallbackToDefault,
            bool logMissing)
        {
            if (!string.IsNullOrWhiteSpace(presetId) && set.TryGetPreset(presetId, out var preset))
                return preset;

            if (!fallbackToDefault)
            {
                if (logWarnings && logMissing && !string.IsNullOrWhiteSpace(presetId))
                {
                    Debug.LogWarning(
                        $"[MouseCursorController] Preset '{presetId}' does not exist in set '{set.SetId}'.");
                }
                return null;
            }

            var fallback = set.GetDefaultPreset();
            if (fallback == null && logWarnings && logMissing)
            {
                Debug.LogWarning(
                    $"[MouseCursorController] Cursor set '{set.SetId}' has no valid default preset configured.");
            }

            return fallback;
        }

        bool TryGetPlatformCursorData(CursorPreset preset, out Texture2D texture, out Vector2 hotSpot, out CursorMode mode)
        {
            texture = preset.Texture;
            hotSpot = preset.HotSpot;
            mode = preset.Mode;

            if (texture == null)
                return false;

            if (!IsLinuxPlatform())
            {
                hotSpot = ClampHotspot(hotSpot, texture.width, texture.height);
                return true;
            }

            if (linuxForceSoftwareCursor)
                mode = CursorMode.ForceSoftware;

            var desiredSize = linuxTargetCursorSize > 0 ? linuxTargetCursorSize : linuxMaxCursorSize;
            var clampedMaxSize = Mathf.Max(16, desiredSize);
            var maxDimension = Mathf.Max(texture.width, texture.height);

            if (maxDimension <= clampedMaxSize)
                return true;

            if (!texture.isReadable)
            {
                if (logWarnings)
                {
                    Debug.LogWarning(
                        "[MouseCursorController] Cursor texture is not readable; cannot downscale for Linux. " +
                        "Enable Read/Write in the texture import settings. Forcing software cursor to avoid OS scaling.");
                }
                mode = CursorMode.ForceSoftware;
                return true;
            }

            var scale = clampedMaxSize / (float)maxDimension;
            var targetWidth = Mathf.Max(1, Mathf.RoundToInt(texture.width * scale));
            var targetHeight = Mathf.Max(1, Mathf.RoundToInt(texture.height * scale));
            var scaledHotSpot = hotSpot * scale;

            if (cachedScaledTexture != null)
                Destroy(cachedScaledTexture);

            cachedScaledTexture = CreateScaledTexture(texture, targetWidth, targetHeight);

            texture = cachedScaledTexture;
            hotSpot = ClampHotspot(scaledHotSpot, targetWidth, targetHeight);
            return true;
        }

        bool IsLinuxPlatform() =>
            Application.platform == RuntimePlatform.LinuxEditor ||
            Application.platform == RuntimePlatform.LinuxPlayer;

        static Vector2 ClampHotspot(Vector2 hotSpot, int width, int height)
        {
            var clampedX = Mathf.Clamp(hotSpot.x, 0, Mathf.Max(0, width - 1));
            var clampedY = Mathf.Clamp(hotSpot.y, 0, Mathf.Max(0, height - 1));
            return new Vector2(clampedX, clampedY);
        }

        static Texture2D CreateScaledTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            var result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, mipChain: false)
            {
                name = string.IsNullOrWhiteSpace(source.name) ? "Cursor_LinuxScaled" : $"{source.name}_LinuxScaled",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < targetHeight; y++)
            {
                var v = (y + 0.5f) / targetHeight;
                for (int x = 0; x < targetWidth; x++)
                {
                    var u = (x + 0.5f) / targetWidth;
                    result.SetPixel(x, y, source.GetPixelBilinear(u, v));
                }
            }

            result.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return result;
        }

        void LogInfo(string message)
        {
            if (logInfo)
                Debug.Log($"[MouseCursorController] {message}");
        }
    }

    /// <summary>Event raised to request a cursor change anywhere in code.</summary>
    public readonly struct CursorChangeRequestEvent : IEvent
    {
        public string CursorSetId { get; }
        public string PresetId { get; }
        public bool AllowFallbackToDefaultPreset { get; }
        public bool ForceRefresh { get; }
        public bool? CursorVisibility { get; }
        public CursorLockMode? LockMode { get; }

        public CursorChangeRequestEvent(
            string presetId,
            string cursorSetId = null,
            bool allowFallbackToDefaultPreset = true,
            bool? cursorVisibility = null,
            CursorLockMode? lockMode = null,
            bool forceRefresh = false)
        {
            PresetId = presetId;
            CursorSetId = cursorSetId;
            AllowFallbackToDefaultPreset = allowFallbackToDefaultPreset;
            CursorVisibility = cursorVisibility;
            LockMode = lockMode;
            ForceRefresh = forceRefresh;
        }
    }

    /// <summary>Raised every time the controller switches to a new cursor set.</summary>
    public readonly struct CursorSetChangedEvent : IEvent
    {
        public string CursorSetId { get; }

        public CursorSetChangedEvent(string cursorSetId) => CursorSetId = cursorSetId;
    }

    /// <summary>Raised after a cursor preset has been successfully applied.</summary>
    public readonly struct CursorPresetChangedEvent : IEvent
    {
        public string CursorSetId { get; }
        public string PresetId { get; }
        public CursorPreset Preset { get; }

        public CursorPresetChangedEvent(string cursorSetId, string presetId, CursorPreset preset)
        {
            CursorSetId = cursorSetId;
            PresetId = presetId;
            Preset = preset;
        }
    }
}
