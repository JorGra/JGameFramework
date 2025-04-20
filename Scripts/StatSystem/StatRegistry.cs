using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Stats/Stat Registry", fileName = "NewStatRegistry")]
public class StatRegistry : ScriptableObject
{
    [Tooltip("List of all stat definitions available in the game.")]
    public List<StatDefinition> statDefinitions;

    // Lookup table from string key → StatDefinition
    private Dictionary<string, StatDefinition> lookupByKey;

    // DTOs for parsing JSON extras
    [Serializable]
    private class StatDefJson
    {
        public string key;
        public string statName;
        public float defaultValue;
    }

    [Serializable]
    private class StatDefListJson
    {
        public List<StatDefJson> stats;
    }

    private void OnEnable()
    {
        // 1) Build from SO assets
        lookupByKey = new Dictionary<string, StatDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in statDefinitions)
        {
            if (def == null)
                continue;

            if (string.IsNullOrWhiteSpace(def.key))
            {
                Debug.LogWarning($"StatDefinition '{def.name}' missing a key; skipping.");
                continue;
            }

            if (lookupByKey.ContainsKey(def.key))
            {
                Debug.LogError($"Duplicate StatDefinition key '{def.key}' in registry.");
                continue;
            }

            lookupByKey.Add(def.key, def);
        }

        // 2) Load additional stats from JSON
        var ta = Resources.Load<TextAsset>("StatDefinitions");
        if (ta != null)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<StatDefListJson>(ta.text);
                foreach (var dto in wrapper.stats)
                {
                    // skip blank or duplicate keys
                    if (string.IsNullOrWhiteSpace(dto.key))
                    {
                        Debug.LogWarning("StatRegistry JSON entry missing key; skipping.");
                        continue;
                    }
                    if (lookupByKey.ContainsKey(dto.key))
                    {
                        Debug.LogWarning($"StatRegistry: JSON stat key '{dto.key}' already exists; skipping.");
                        continue;
                    }

                    // dynamically create a new StatDefinition
                    var so = ScriptableObject.CreateInstance<StatDefinition>();
                    so.key = dto.key;
                    so.statName = dto.statName;
                    so.defaultValue = dto.defaultValue;

                    statDefinitions.Add(so);
                    lookupByKey.Add(so.key, so);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"StatRegistry: Failed to parse StatDefinitions.json: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("StatDefinitions.json not found in Resources—no dynamic stats loaded.");
        }
    }

    /// <summary>
    /// Retrieves a stat definition by its unique string key.
    /// </summary>
    public StatDefinition Get(string key)
    {
        if (lookupByKey != null && lookupByKey.TryGetValue(key, out var def))
            return def;

        throw new KeyNotFoundException($"StatDefinition with key '{key}' not found in registry.");
    }
}
