using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds current stats + modifiers for an entity.
/// Now keys base values by statDefinition.key instead of integer ID.
/// </summary>
public class Stats
{
    public StatsMediator Mediator { get; private set; }

    // Stores base values keyed by StatDefinition.key
    readonly Dictionary<string, float> baseStats;

    /// <summary>
    /// Constructs the Stats object using the provided profile.
    /// If profile is null, falls back to registry defaults.
    /// </summary>
    public Stats(StatsProfile profile)
    {
        Mediator = new StatsMediator();
        baseStats = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        if (profile != null)
        {
            foreach (var entry in profile.statEntries)
            {
                if (entry.statDefinition != null &&
                    !string.IsNullOrWhiteSpace(entry.statDefinition.key))
                {
                    baseStats[entry.statDefinition.key] = entry.baseValue;
                }
                else
                {
                    Debug.LogWarning(
                      $"Stats ctor: missing key on stat definition in profile {profile.name}");
                }
            }
        }
        else
        {
            // Fallback: load all defaults from registry
            var registry = StatRegistryProvider.Instance;
            if (registry != null)
            {
                foreach (var def in registry.statDefinitions)
                {
                    if (!string.IsNullOrWhiteSpace(def.key))
                        baseStats[def.key] = def.defaultValue;
                }
            }
            else
            {
                Debug.LogError("Stats ctor: StatRegistry missing—cannot load default stats.");
            }
        }
    }

    /// <summary>
    /// Retrieves the final value for the given stat.
    /// </summary>
    public float GetStat(StatDefinition statDefinition)
    {
        if (statDefinition == null || string.IsNullOrWhiteSpace(statDefinition.key))
        {
            Debug.LogError("GetStat: invalid StatDefinition or missing key.");
            return 0f;
        }

        if (!baseStats.TryGetValue(statDefinition.key, out var baseValue))
            baseValue = statDefinition.defaultValue;

        var q = new Query(statDefinition, baseValue);
        Mediator.PerfromQuery(this, q);
        return q.Value;
    }
}
