using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JGameFramework.Saving
{
    /// <summary>
    /// Synchronous static save API.
    /// Non-cached cases write to disk immediately. Cached cases write to memory
    /// and flush to disk on <see cref="Flush"/>, app pause, or app quit.
    /// </summary>
    public static class SaveSystem
    {
        public static bool IsInitialized { get; private set; }
        public static string ActiveSlotId { get; private set; }

        private static ISaveBackend backend;
        private static bool isFallbackInit;
        private static SaveSystemRuntimeHook hook;

        private static readonly HashSet<SaveCaseId> cachedCases = new();
        private static readonly Dictionary<(string slot, SaveCaseId caseId), object> cache = new();
        private static readonly Dictionary<(string slot, SaveCaseId caseId), Action> pendingWrites = new();

        #region Initialization

        /// <summary>
        /// Initialize with a backend and optional config.
        /// Config controls default slot ID and per-case caching.
        /// </summary>
        public static void Init(ISaveBackend backend, SaveConfig config = null)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            if (IsInitialized && !isFallbackInit)
                throw new InvalidOperationException("[SaveSystem] Init was already called.");
            if (IsInitialized && isFallbackInit)
                ResetState();

            SaveSystem.backend = backend;

            if (config != null)
            {
                ActiveSlotId = string.IsNullOrWhiteSpace(config.DefaultSlotId)
                    ? "slot_default"
                    : config.DefaultSlotId;

                foreach (var c in config.Cases)
                {
                    if (c.Cached)
                        cachedCases.Add(c.CaseId);
                }
            }
            else
            {
                ActiveSlotId = "slot_default";
            }

            AttachRuntimeHook();
            IsInitialized = true;
            isFallbackInit = false;
        }

        /// <summary>
        /// Mark additional cases as cached from code.
        /// Additive with cases already marked in SaveConfig.
        /// </summary>
        public static void SetCached(params SaveCaseId[] caseIds)
        {
            foreach (var id in caseIds)
                cachedCases.Add(id);
        }

        public static void UseSlot(string slotId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(slotId)) throw new ArgumentNullException(nameof(slotId));
            if (string.Equals(ActiveSlotId, slotId, StringComparison.OrdinalIgnoreCase)) return;

            Flush();
            cache.Clear();
            ActiveSlotId = slotId;
        }

        #endregion

        #region Public API

        public static void Save<T>(SaveCaseId caseId, T value)
        {
            EnsureInitialized();
            var slot = ActiveSlotId;

            if (cachedCases.Contains(caseId))
            {
                cache[(slot, caseId)] = value;
                pendingWrites[(slot, caseId)] = () => backend.Save(slot, caseId.Value, value);
            }
            else
            {
                try
                {
                    backend.Save(slot, caseId.Value, value);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveSystem] Save failed for '{caseId.Value}': {ex}");
                }
            }
        }

        public static T Load<T>(SaveCaseId caseId, T defaultValue = default)
        {
            EnsureInitialized();
            var slot = ActiveSlotId;

            if (cachedCases.Contains(caseId)
                && cache.TryGetValue((slot, caseId), out var cached)
                && cached is T typed)
            {
                return typed;
            }

            T result;
            try
            {
                result = backend.Load(slot, caseId.Value, defaultValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Load failed for '{caseId.Value}': {ex}");
                return defaultValue;
            }

            if (cachedCases.Contains(caseId) && result != null)
                cache[(slot, caseId)] = result;

            return result;
        }

        public static bool Exists(SaveCaseId caseId)
        {
            EnsureInitialized();
            try
            {
                return backend.Exists(ActiveSlotId, caseId.Value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Exists failed for '{caseId.Value}': {ex}");
                return false;
            }
        }

        public static void Delete(SaveCaseId caseId)
        {
            EnsureInitialized();
            cache.Remove((ActiveSlotId, caseId));
            pendingWrites.Remove((ActiveSlotId, caseId));

            try
            {
                backend.Delete(ActiveSlotId, caseId.Value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Delete failed for '{caseId.Value}': {ex}");
            }
        }

        public static void DeleteSlot(string slotId)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(slotId)) return;

            // Discard pending writes and cache for this slot — data is being deleted
            var pendingKeys = pendingWrites.Keys
                .Where(k => k.slot.Equals(slotId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in pendingKeys)
                pendingWrites.Remove(key);

            var cacheKeys = cache.Keys
                .Where(k => k.slot.Equals(slotId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in cacheKeys)
                cache.Remove(key);

            try
            {
                backend.DeleteSlot(slotId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] DeleteSlot failed: {ex}");
            }

            if (string.Equals(ActiveSlotId, slotId, StringComparison.OrdinalIgnoreCase))
                ActiveSlotId = "slot_default";
        }

        /// <summary>
        /// Write all pending cached saves to disk.
        /// Called automatically on app pause and quit.
        /// </summary>
        public static void Flush()
        {
            if (!IsInitialized || pendingWrites.Count == 0) return;

            var writes = new List<Action>(pendingWrites.Values);
            pendingWrites.Clear();

            foreach (var write in writes)
            {
                try
                {
                    write();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveSystem] Flush write failed: {ex}");
                }
            }
        }

        #endregion

        #region Internals

        private static void EnsureInitialized() => AutoInitIfNeeded();

        private static void AutoInitIfNeeded()
        {
            if (IsInitialized) return;
            isFallbackInit = true;
            Init(new MemoryBackend());
            Debug.LogWarning("[SaveSystem] Init was not called before use; initialized with fallback in-memory backend. Call SaveSystem.Init explicitly at startup.");
        }

        private static void AttachRuntimeHook()
        {
            if (hook != null) return;
            var go = new GameObject("SaveSystemRuntimeHook");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            hook = go.AddComponent<SaveSystemRuntimeHook>();
        }

        private static void ResetState()
        {
            backend = null;
            cache.Clear();
            cachedCases.Clear();
            pendingWrites.Clear();
            ActiveSlotId = null;
            IsInitialized = false;
        }

        #endregion
    }

    internal sealed class SaveSystemRuntimeHook : MonoBehaviour
    {
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveSystem.Flush();
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Flush();
        }
    }
}
