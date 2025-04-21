using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats
{
    public StatsMediator Mediator { get; private set; }
    // Stores base values keyed by StatDefinition ID.
    readonly Dictionary<int, float> baseStats;

    /// <summary>
    /// Constructs the Stats object using the provided UnitStatsProfile.
    /// This profile defines which stats are present on the unit and their base values.
    /// </summary>
    public Stats(StatsProfile profile)
    {
        Mediator = new StatsMediator();
        baseStats = new Dictionary<int, float>();

        if (profile != null)
        {
            foreach (var entry in profile.statEntries)
            {
                if (entry.statDefinition != null)
                {
                    baseStats[entry.statDefinition.id] = entry.baseValue;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"A stat entry in profile '{profile.name}' is missing a StatDefinition.");
                }
            }
        }
        else
        {
            // Fallback: populate baseStats using global definitions.
            try
            {
                var registry = StatRegistryProvider.Instance;
                if (registry != null && registry.statDefinitions != null)
                {
                    foreach (var def in registry.statDefinitions)
                    {
                        if (def != null)
                        {
                            baseStats[def.id] = def.defaultValue;
                        }
                    }
                }
                else
                {
                    Debug.LogError("Global StatRegistry is missing or misconfigured. No base stats could be loaded.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading global stat defaults: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Retrieves the final value for the given stat.
    /// If the stat is not defined in the profile, the default value from the StatDefinition is used.
    /// </summary>
    public float GetStat(StatDefinition statDefinition)
    {
        if (statDefinition == null)
        {
            UnityEngine.Debug.LogError("Null StatDefinition passed to GetStat.");
            return 0f;
        }

        float baseValue;
        if (!baseStats.TryGetValue(statDefinition.id, out baseValue))
        {
            baseValue = statDefinition.defaultValue;
        }

        Query q = new Query(statDefinition, baseValue);
        Mediator.PerfromQuery(this, q);
        return q.Value;
    }
}