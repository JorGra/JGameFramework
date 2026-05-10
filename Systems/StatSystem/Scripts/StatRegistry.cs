// StatRegistry.cs
// Game-agnostic registry that exposes IStatDefinition, populated from the
// content pipeline (StatDef : ContentDef, IStatDefinition).
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Stats/Stat Registry", fileName = "NewStatRegistry")]
public class StatRegistry : ScriptableObject
{
    [NonSerialized] private Dictionary<string, IStatDefinition> _byKey;

    public int Count => _byKey?.Count ?? 0;

    public IEnumerable<IStatDefinition> All
        => _byKey != null ? _byKey.Values : Array.Empty<IStatDefinition>();

    /// <summary>
    /// Rebuilds the lookup index from the provided runtime (content-pipeline) stats.
    /// </summary>
    public void RebuildIndex(IEnumerable<IStatDefinition> runtimeStats)
    {
        _byKey = new Dictionary<string, IStatDefinition>(StringComparer.OrdinalIgnoreCase);

        if (runtimeStats != null)
        {
            foreach (var def in runtimeStats)
                TryAdd(def);
        }

        Debug.Log($"[StatRegistry] Rebuild complete. Total stats: {_byKey.Count}");
    }

    private void TryAdd(IStatDefinition def)
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
            _byKey[key] = def;
        }
        else
        {
            _byKey.Add(key, def);
        }
    }

    public IStatDefinition Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogError("[StatRegistry] Get: empty or null key.");
            return null;
        }
        if (_byKey != null && _byKey.TryGetValue(key, out var def))
            return def;

        Debug.LogWarning($"[StatRegistry] Get: unknown stat key '{key}'.");
        return null;
    }

    public bool TryGet(string key, out IStatDefinition def)
    {
        def = null;
        if (string.IsNullOrWhiteSpace(key) || _byKey == null) return false;
        return _byKey.TryGetValue(key, out def);
    }
}
