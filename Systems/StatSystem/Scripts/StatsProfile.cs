using JG.GameContent;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A profile that defines the base stat values for a unit.
/// Stores stat keys (string) so it can be saved as an asset; resolves to IStatDefinition at runtime/editor.
/// </summary>
[CreateAssetMenu(menuName = "Gameplay/Stats/Stats Profile", fileName = "NewStatsProfile")]
public class StatsProfile : ScriptableObject
{
    [Serializable]
    public struct StatEntry
    {
        [Tooltip("Registry/content key (usually the ContentDef Id).")]
        public string statKey;

        [NonSerialized] public IStatDefinition statDefinition;

        [Tooltip("The base value for this unit for the given stat.")]
        public float baseValue;

        public IStatDefinition Resolve()
        {
            if (!string.IsNullOrWhiteSpace(statKey))
            {
                if (statDefinition == null || !string.Equals(statDefinition.Key, statKey, StringComparison.OrdinalIgnoreCase))
                {
                    var reg = StatRegistryProvider.Instance?.Registry;
                    statDefinition = reg?.Get(statKey);
                }
            }
            return statDefinition;
        }

        public string DisplayName
        {
            get
            {
                var def = Resolve();
                if (def != null && !string.IsNullOrWhiteSpace(def.StatName)) return def.StatName;
                return string.IsNullOrWhiteSpace(statKey) ? "Undefined Stat" : statKey;
            }
        }
    }

    [Tooltip("List of stat entries that define this unit's base stats.")]
    public List<StatEntry> statEntries = new List<StatEntry>();

    private void OnEnable() => ResolveAll();

    public void ResolveAll()
    {
        for (int i = 0; i < statEntries.Count; i++)
        {
            var e = statEntries[i];
            e.Resolve();
            statEntries[i] = e;
        }
    }

    public static StatsProfile BuildStatsProfile(List<StatSpec> def)
    {
        var profile = CreateInstance<StatsProfile>();
        var provider = StatRegistryProvider.Instance;
        var registry = provider.Registry;

        if (registry.Count == 0)
            provider.RefreshFromContent();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in def)
        {
            if (string.IsNullOrWhiteSpace(s.statId))
                throw new Exception($"BuildStatsProfile: empty statId in baseStats.");

            if (!seen.Add(s.statId))
                throw new Exception($"BuildStatsProfile: duplicate statId '{s.statId}' in baseStats.");

            if (!registry.TryGet(s.statId, out var stat) || stat == null)
            {
                provider.RefreshFromContent();
                if (!registry.TryGet(s.statId, out stat) || stat == null)
                    throw new Exception($"BuildStatsProfile: unknown statId '{s.statId}'.");
            }

            profile.statEntries.Add(new StatEntry
            {
                statKey = s.statId,
                statDefinition = stat, // cached for runtime use; not serialized
                baseValue = s.baseValue
            });
        }

        return profile;
    }
}

// Json DTO Mapper
[Serializable]
public struct StatSpec
{
    // Content integration is optional; keep this as a plain string to avoid hard dependencies.
    [IdReference(typeof(StatDef))]
    public string statId;     // e.g. "maxhealth"
    public float baseValue;   // e.g. 120
}

