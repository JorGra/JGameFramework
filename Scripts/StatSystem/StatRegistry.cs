// StatRegistry.cs (UPDATED)
// Game-agnostic registry that exposes IStatDefinition and can be populated
// from both legacy ScriptableObjects (StatDefinition) and the new content pipeline
// (StatDef : ContentDef, IStatDefinition). All JSON importing has been removed.
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Stats/Stat Registry", fileName = "NewStatRegistry")]
public class StatRegistry : ScriptableObject
{
    // === Legacy baked-in assets (kept for compatibility) ===
    // These can still be populated via inspector with old StatDefinition SOs.
    [Tooltip("Optional baked stat definitions (legacy ScriptableObjects). " +
             "These are merged with content pipeline stats at runtime.")]
    public List<StatDefinition> statDefinitions = new();

    // === Runtime index ===
    [NonSerialized] private Dictionary<string, IStatDefinition> _byKey;

    /// <summary>Returns the number of registered stats (after last rebuild).</summary>
    public int Count => _byKey?.Count ?? 0;

    /// <summary>Enumerate all registered stat definitions.</summary>
    public IEnumerable<IStatDefinition> All
        => _byKey != null ? _byKey.Values : Array.Empty<IStatDefinition>();

    /// <summary>
    /// Rebuilds the lookup index using the currently serialized legacy SOs and the
    /// provided runtime (content-pipeline) stats. If <paramref name="runtimeOverridesBaked"/>
    /// is true, definitions from the content pipeline replace same-key baked assets.
    /// </summary>
    public void RebuildIndex(IEnumerable<IStatDefinition> runtimeStats,
                             bool runtimeOverridesBaked = true)
    {
        _byKey = new Dictionary<string, IStatDefinition>(StringComparer.OrdinalIgnoreCase);

        // 1) Add baked (legacy) ScriptableObjects
        if (statDefinitions != null)
        {
            foreach (var def in statDefinitions)
                TryAdd(def, allowOverride: false);
        }

        // 2) Merge runtime/content-pipeline stats
        if (runtimeStats != null)
        {
            foreach (var def in runtimeStats)
                TryAdd(def, allowOverride: runtimeOverridesBaked);
        }

        Debug.Log($"[StatRegistry] Rebuild complete. Total stats: {_byKey.Count}");
    }

    private void TryAdd(IStatDefinition def, bool allowOverride)
    {
        if (def == null) return;
        var key = def.Key;

        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning($"[StatRegistry] Skipping definition with empty key: {def}");
            return;
        }

        if (_byKey.TryGetValue(key, out var existing))
        {
            if (ReferenceEquals(existing, def)) return;

            if (allowOverride)
            {
                _byKey[key] = def;
                Debug.Log($"[StatRegistry] Overrode stat '{key}' with runtime definition.");
            }
            else
            {
                Debug.LogWarning($"[StatRegistry] Duplicate key '{key}' ignored (keeping first).");
            }
        }
        else
        {
            _byKey.Add(key, def);
        }
    }

    /// <summary>Returns the stat definition for <paramref name="key"/> or null if not found.</summary>
    public IStatDefinition Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogError("[StatRegistry] Get: empty or null key.");
            return null;
        }
        if (_byKey != null && _byKey.TryGetValue(key, out var def))
        {
            return def;
        }
        else
        {
            Debug.LogWarning($"[StatRegistry] Get: unknown stat key '{key}'.");
            return null;
        }
    }

    /// <summary>Try to get a stat definition by key.</summary>
    public bool TryGet(string key, out IStatDefinition def)
    {
        def = null;
        if (string.IsNullOrWhiteSpace(key) || _byKey == null) return false;
        return _byKey.TryGetValue(key, out def);
    }

#if UNITY_EDITOR
    // Helpful in editor when you tweak baked lists.
    private void OnValidate()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (statDefinitions == null) return;

        foreach (var d in statDefinitions)
        {
            if (d == null) continue;
            if (!seen.Add(d.Key))
                Debug.LogWarning($"[StatRegistry] Duplicate baked key '{d.Key}'");
        }
    }
#endif
}
