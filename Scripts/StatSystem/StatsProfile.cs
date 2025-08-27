using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A profile that defines the base stat values for a unit.
/// A unit can have only some of the available stats.
/// </summary>
[CreateAssetMenu(menuName = "Gameplay/Stats/Stats Profile", fileName = "NewStatsProfile")]
public class StatsProfile : ScriptableObject
{
    [Serializable]
    public struct StatEntry
    {
        [Tooltip("Reference to the global stat definition.")]
        public IStatDefinition statDefinition;

        [Tooltip("The base value for this unit for the given stat.")]
        public float baseValue;
    }

    [Tooltip("List of stat entries that define this unit's base stats.")]
    public List<StatEntry> statEntries = new List<StatEntry>();

    public static StatsProfile BuildStatsProfile(List<StatSpec> def)
    {
        var profile = ScriptableObject.CreateInstance<StatsProfile>();

        // Optional: detect duplicates early
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in def)
        {
            if (string.IsNullOrWhiteSpace(s.statId))
                throw new Exception($"BuildStatsProfile '{s.statId}': empty statId in baseStats.");

            if (!seen.Add(s.statId))
                throw new Exception($"BuildStatsProfile '{s.statId}': duplicate statId '{s.statId}' in baseStats.");

            var stat = StatRegistryProvider.Instance.Registry.Get(s.statId);
            if (stat == null)
                throw new Exception($"BuildStatsProfile '{s.statId}': unknown statId '{s.statId}'.");

            profile.statEntries.Add(new StatsProfile.StatEntry
            {
                statDefinition = stat,
                baseValue = s.baseValue
            });
        }

        return profile;
    }
}
//Json DTO Mapper
[Serializable]
public struct StatSpec
{
    public string statId;     // e.g. "com.mygame.maxhealth"
    public float baseValue;   // e.g. 120
}
