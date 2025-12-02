using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds current stats + modifiers for an entity, using defaults from StatRegistry.
/// </summary>
public class Stats
{
    public StatsMediator Mediator { get; private set; }
    private readonly Dictionary<string, float> baseStats;

    public Stats()
    {
        Mediator = new StatsMediator();
        baseStats = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        var reg = StatRegistryProvider.Instance?.Registry;
        if (reg != null)
        {
            // Fill base values from content-driven defaults
            foreach (var def in reg.All)
            {
                if (!string.IsNullOrWhiteSpace(def.Key))
                    baseStats[def.Key] = def.DefaultValue;
            }
        }
        else
        {
            Debug.LogError("Stats ctor: StatRegistry missing—cannot load default stats.");
        }
    }

    public Stats(StatsProfile profile) : this()
    {
        if (profile?.statEntries != null)
        {
            foreach (var e in profile.statEntries)
            {
                if (!string.IsNullOrWhiteSpace(e.statKey))
                    baseStats[e.statKey] = e.baseValue;
                else
                    Debug.LogWarning($"A stat entry in profile '{profile.name}' is missing a key.");
            }
        }
    }

    /// <summary>Resolve final stat value by key (base + modifiers).</summary>
    public float GetStat(string statKey)
    {
        if (string.IsNullOrWhiteSpace(statKey))
        {
            Debug.LogError("GetStat: missing stat key.");
            return 0f;
        }

        float baseValue;
        if (!baseStats.TryGetValue(statKey, out baseValue))
        {
            // Fallback to content default if not present in base map
            var def = StatRegistryProvider.Instance?.Registry?.Get(statKey);
            baseValue = def?.DefaultValue ?? 0f;
        }

        var query = new Query(statKey, baseValue);
        Mediator.PerfromQuery(this, query);
        return query.Value;
    }

    public void SetBase(string statKey, float value)
    {
        if (string.IsNullOrWhiteSpace(statKey))
        {
            Debug.LogError("SetBase: missing stat key.");
            return;
        }
        baseStats[statKey] = value;
    }

    public Dictionary<string, float> GetAllStats()
    {
        var allStats = new Dictionary<string, float>(baseStats);
        var r = new Dictionary<string, float>();
        foreach (var stat in allStats)
        {
            var v = GetStat(stat.Key);
            r[stat.Key] = v;
        }
        return r;
    }
}
