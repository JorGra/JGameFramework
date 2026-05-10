using System;
using System.Collections.Generic;
using JG.Scaling;
using UnityEngine;

/// <summary>
/// Holds current stats + modifiers for an entity, using defaults from StatRegistry.
/// </summary>
public class Stats : IStatProvider
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
            Debug.LogError("Stats ctor: StatRegistry missing�cannot load default stats.");
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

    [ThreadStatic] private static HashSet<string> _evaluating;

    /// <summary>Resolve final stat value by key (base + modifiers).</summary>
    public float GetStat(string statKey)
    {
        if (string.IsNullOrWhiteSpace(statKey))
        {
            Debug.LogError("GetStat: missing stat key.");
            return 0f;
        }

        float baseValue = GetBase(statKey);

        // Cycle guard: if we re-enter for the same stat (e.g. ScaledValue.Evaluate
        // pulls a stat that itself scales on this stat), short-circuit to the raw
        // base value instead of recursing.
        _evaluating ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_evaluating.Contains(statKey))
        {
            Debug.LogWarning($"Scaling cycle detected on '{statKey}'; returning base value {baseValue}.");
            return baseValue;
        }

        _evaluating.Add(statKey);
        try
        {
            var query = new Query(statKey, baseValue, this);
            Mediator.PerfromQuery(this, ref query);
            return query.Value;
        }
        finally
        {
            _evaluating.Remove(statKey);
        }
    }

    /// <summary>Returns the pre-modifier base value for a stat (profile or registry default).</summary>
    public float GetBase(string statKey)
    {
        if (string.IsNullOrWhiteSpace(statKey)) return 0f;
        if (baseStats.TryGetValue(statKey, out var v)) return v;
        var def = StatRegistryProvider.Instance?.Registry?.Get(statKey);
        return def?.DefaultValue ?? 0f;
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
