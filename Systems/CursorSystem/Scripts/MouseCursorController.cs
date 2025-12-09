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

        [Header("Debug")]
        [SerializeField] bool logWarnings = true;

        readonly Dictionary<string, CursorSetDefinition> setLookup =
            new Dictionary<string, CursorSetDefinition>(StringComparer.OrdinalIgnoreCase);

        CursorSetDefinition activeSet;
        CursorPreset activePreset;
        string activeSetId;
        string activePresetId;

        EventSubscription<CursorChangeRequestEvent> changeSubscription;

        public string ActiveSetId => activeSetId;
        public string ActivePresetId => activePresetId;

        void Awake()
        {
            BuildLookup();

            changeSubscription = EventBus<CursorChangeRequestEvent>.Subscribe(HandleCursorChangeRequest, this);

            if (applyDefaultOnAwake)
            {
                ApplyPreset(defaultPresetId, defaultCursorSetId, fallbackToDefault: true, forceRefresh: true, logMissing: false);
            }
        }

        void OnDestroy()
        {
            changeSubscription?.Dispose();
            changeSubscription = null;
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

            Cursor.SetCursor(targetPreset.Texture, targetPreset.HotSpot, targetPreset.Mode);
            if (targetPreset.OverrideCursorVisibility)
                Cursor.visible = targetPreset.CursorVisible;

            var previousSetId = activeSetId;
            activeSet = targetSet;
            activePreset = targetPreset;
            activeSetId = targetSet.SetId;
            activePresetId = !string.IsNullOrWhiteSpace(targetPreset.PresetId)
                ? targetPreset.PresetId
                : (presetId ?? defaultPresetId);

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
