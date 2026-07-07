using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.CursorSystem
{
    /// <summary>
    /// Central mouse cursor manager. Resolves the highest-priority claim on the
    /// <see cref="CursorClaimStack"/> and shows it through an <see cref="ICursorPresenter"/>.
    /// Sources (scene selectors, hover probe, game-state adapters) push claims instead of
    /// imperatively setting the cursor, so releasing a claim restores whatever lies beneath.
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

        [Header("Presentation")]
        [Tooltip("Auto = overlay cursor on Linux (avoids dual-cursor/OS-scaling issues), hardware cursor elsewhere.")]
        [SerializeField] CursorPresenterMode presenterMode = CursorPresenterMode.Auto;
        [Tooltip("Overlay presenter: cursor height as a fraction of screen height (resolution independent).")]
        [SerializeField, Range(0.01f, 0.15f)] float overlayCursorHeightFraction = 0.035f;

        [Header("Hardware Presenter — Linux Tweaks")]
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
        bool? lastClaimVisibility;
        CursorLockMode? lastLockMode;

        ICursorPresenter presenter;
        CursorClaimHandle defaultClaim;
        CursorClaimHandle legacyEventClaim;
#pragma warning disable 618 // legacy compatibility path
        EventSubscription<CursorChangeRequestEvent> changeSubscription;
#pragma warning restore 618
        bool initialized;

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
                    // so other behaviours on the same GameObject (e.g., hover probes) keep running.
                    Destroy(this);
                    return;
                }

                persistentInstance = this;
                DontDestroyOnLoad(gameObject);
            }

            BuildLookup();
            presenter = CreatePresenter();

            CursorClaimStack.Changed += OnClaimsChanged;

#pragma warning disable 618 // legacy compatibility path
            changeSubscription = EventBus<CursorChangeRequestEvent>.Subscribe(HandleCursorChangeRequest, this);
#pragma warning restore 618

            if (applyDefaultOnAwake)
            {
                // Pushed first, so any later claim at the same Default priority outranks it.
                defaultClaim = CursorClaimStack.Push(
                    defaultPresetId,
                    defaultCursorSetId,
                    CursorLayer.Default,
                    owner: this);
            }

            initialized = true;
            // Claims may have been pushed before this controller existed — resolve them now.
            ResolveAndApply(forceRefresh: true);
        }

        void Update() => presenter?.Tick();

        void OnDestroy()
        {
            if (persistentInstance == this)
                persistentInstance = null;

            if (!initialized)
                return;

            CursorClaimStack.Changed -= OnClaimsChanged;

            changeSubscription?.Dispose();
            changeSubscription = null;

            legacyEventClaim?.Dispose();
            legacyEventClaim = null;

            defaultClaim?.Dispose();
            defaultClaim = null;

            presenter?.Cleanup();
            presenter = null;
        }

        void OnValidate()
        {
            if (!Application.isPlaying || !initialized)
                return;

            BuildLookup();
            ResolveAndApply(forceRefresh: true);
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

        ICursorPresenter CreatePresenter()
        {
            var mode = presenterMode;
            if (mode == CursorPresenterMode.Auto)
            {
                var isLinux = Application.platform == RuntimePlatform.LinuxEditor ||
                              Application.platform == RuntimePlatform.LinuxPlayer;
                mode = isLinux ? CursorPresenterMode.Overlay : CursorPresenterMode.Hardware;
            }

            LogInfo($"Presenter: {mode}");

            return mode == CursorPresenterMode.Overlay
                ? new OverlayCursorPresenter(transform, overlayCursorHeightFraction)
                : (ICursorPresenter)new HardwareCursorPresenter(
                    linuxMaxCursorSize, linuxTargetCursorSize, linuxForceSoftwareCursor, logWarnings);
        }

        void OnClaimsChanged() => ResolveAndApply(forceRefresh: false);

        void ResolveAndApply(bool forceRefresh)
        {
            if (!CursorClaimStack.TryGetWinner(out var winner))
                return;

            if (!ApplyClaim(winner, forceRefresh) && logWarnings)
            {
                Debug.LogWarning(
                    $"[MouseCursorController] Could not apply claim preset '{winner.PresetId ?? "<default>"}' " +
                    $"from set '{winner.SetId ?? ActiveSetId ?? defaultCursorSetId}'.");
            }
        }

        /// <summary>Legacy fire-and-forget path: each request replaces a single Scene-layer claim.</summary>
#pragma warning disable 618
        void HandleCursorChangeRequest(CursorChangeRequestEvent request)
        {
            LogInfo($"Legacy request: set='{request.CursorSetId ?? "<null>"}' preset='{request.PresetId ?? "<null>"}' force={request.ForceRefresh}");

            legacyEventClaim?.Dispose();
            legacyEventClaim = CursorClaimStack.Push(
                request.PresetId,
                request.CursorSetId,
                CursorLayer.Scene,
                owner: this,
                allowFallback: request.AllowFallbackToDefaultPreset,
                visibility: request.CursorVisibility,
                lockMode: request.LockMode);

            if (request.ForceRefresh)
                ResolveAndApply(forceRefresh: true);
        }
#pragma warning restore 618

        bool ApplyClaim(CursorClaim claim, bool forceRefresh)
        {
            var targetSet = ResolveSet(claim.SetId, logMissing: true);
            if (targetSet == null)
                return false;

            var targetPreset = ResolvePreset(targetSet, claim.PresetId, claim.AllowFallback, logMissing: true);
            if (targetPreset == null)
                return false;

            var samePreset = ReferenceEquals(targetSet, activeSet) && ReferenceEquals(targetPreset, activePreset);
            var sameOverrides = claim.Visibility == lastClaimVisibility && claim.LockMode == lastLockMode;
            if (!forceRefresh && samePreset && sameOverrides)
                return true;

            if (!targetPreset.HasTexture)
            {
                if (logWarnings)
                {
                    Debug.LogWarning(
                        $"[MouseCursorController] Preset '{targetPreset.PresetId}' in set '{targetSet.SetId}' is missing a texture.");
                }
                return false;
            }

            if (!presenter.Apply(targetPreset, claim.Visibility))
                return false;

            if (claim.LockMode.HasValue)
                Cursor.lockState = claim.LockMode.Value;

            lastClaimVisibility = claim.Visibility;
            lastLockMode = claim.LockMode;

            var previousSetId = activeSetId;
            activeSet = targetSet;
            activePreset = targetPreset;
            activeSetId = targetSet.SetId;
            activePresetId = !string.IsNullOrWhiteSpace(targetPreset.PresetId)
                ? targetPreset.PresetId
                : (claim.PresetId ?? defaultPresetId);

            LogInfo($"Applied cursor set='{activeSetId}', preset='{activePresetId}' (claim priority={claim.Priority})");

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

        void LogInfo(string message)
        {
            if (logInfo)
                Debug.Log($"[MouseCursorController] {message}");
        }
    }

    /// <summary>
    /// Legacy fire-and-forget cursor request. Handled as a single replaceable claim at
    /// <see cref="CursorLayer.Scene"/> priority.
    /// </summary>
    [Obsolete("Push a claim via CursorClaimStack.Push(...) instead; claims restore automatically when disposed.", false)]
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
