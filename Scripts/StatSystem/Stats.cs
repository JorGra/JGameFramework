// Stats.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds current stats + modifiers for an entity, using defaults from StatRegistry.
/// </summary>
public class Stats
{
    public StatsMediator Mediator { get; private set; }
    // Stores base values keyed by StatDefinition.key
    private readonly Dictionary<string, float> baseStats;

    /// <summary>
    /// Constructs the Stats object by loading all defaults from the provider.
    /// </summary>
    public Stats()
    {
        Mediator = new StatsMediator();
        baseStats = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        var provider = StatRegistryProvider.Instance;
        if (provider?.Registry != null)
        {
            foreach (var def in provider.Registry.StatDefinitions)
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

    public Stats(StatsProfile statsProfile)
    {
        Mediator = new StatsMediator();
        baseStats = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);


        if (statsProfile != null)
        {
            foreach (var entry in statsProfile.statEntries)
            {
                if (entry.statDefinition != null)
                {
                    baseStats[entry.statDefinition.key] = entry.baseValue;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"A stat entry in profile '{statsProfile.name}' is missing a StatDefinition.");
                }
            }
        }
        else
        {
            // Fallback: populate baseStats using global definitions.
            try
            {
                var provider = StatRegistryProvider.Instance;
                if (provider?.Registry != null)
                {
                    foreach (var def in provider.Registry.StatDefinitions)
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
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading global stat defaults: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Retrieves the final value for the given stat, applying all modifiers.
    /// </summary>
    /// <param name="statDefinition">The definition of the stat to query.</param>
    /// <returns>Base value plus modifiers.</returns>
    public float GetStat(StatDefinition statDefinition)
    {
        if (statDefinition == null || string.IsNullOrWhiteSpace(statDefinition.key))
        {
            Debug.LogError("GetStat: invalid StatDefinition or missing key.");
            return 0f;
        }

        if (!baseStats.TryGetValue(statDefinition.key, out var baseValue))
            baseValue = statDefinition.defaultValue;

        var query = new Query(statDefinition, baseValue);
        Mediator.PerfromQuery(this, query);
        return query.Value;
    }
}
