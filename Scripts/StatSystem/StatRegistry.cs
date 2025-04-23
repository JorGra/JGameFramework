// StatRegistry.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Stats/Stat Registry", fileName = "NewStatRegistry")]
public class StatRegistry : ScriptableObject
{
    [Tooltip("List of all stat definitions available in the game.")]
    [SerializeField] private List<StatDefinition> statDefinitions = new List<StatDefinition>();

    private Dictionary<string, StatDefinition> lookupByKey;

    #region JSON DTOs
    [Serializable]
    private class StatDefJson { public string key; public string statName; public float defaultValue; }
    [Serializable]
    private class StatDefListJson { public List<StatDefJson> stats; }
    #endregion

    /// <summary>
    /// Initialize by parsing the given JSON text. Clears out any prior dynamic loads.
    /// </summary>
    public void InitializeFromJsonText(string jsonText)
    {
        // 1) Remove any prior dynamic (and null) entries
        statDefinitions.RemoveAll(d => d == null ||
            (d.hideFlags & HideFlags.HideAndDontSave) != 0);

        // 2) Rebuild lookup for any SO-baked definitions
        BuildLookupFromSO();

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            Debug.LogWarning("StatRegistry: empty JSON; no dynamic stats loaded.");
            return;
        }

        StatDefListJson wrapper;
        try
        {
            wrapper = JsonUtility.FromJson<StatDefListJson>(jsonText);
        }
        catch (Exception e)
        {
            Debug.LogError($"StatRegistry: JSON parse error: {e.Message}");
            return;
        }

        if (wrapper?.stats == null)
            return;

        foreach (var dto in wrapper.stats)
        {
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

            // 3) Create a transient SO instance
            var so = ScriptableObject.CreateInstance<StatDefinition>();
            so.hideFlags = HideFlags.HideAndDontSave;
            so.key = dto.key;
            so.statName = dto.statName;
            so.defaultValue = dto.defaultValue;

            statDefinitions.Add(so);
            lookupByKey.Add(so.key, so);
        }
    }

    public StatDefinition Get(string key)
    {
        if (lookupByKey != null && lookupByKey.TryGetValue(key, out var def))
            return def;
        throw new KeyNotFoundException($"StatDefinition with key '{key}' not found.");
    }

    public IReadOnlyList<StatDefinition> StatDefinitions => statDefinitions;

    private void BuildLookupFromSO()
    {
        lookupByKey = new Dictionary<string, StatDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in statDefinitions)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.key))
                continue;

            if (lookupByKey.ContainsKey(def.key))
            {
                Debug.LogError($"Duplicate StatDefinition key '{def.key}' in registry.");
                continue;
            }
            lookupByKey.Add(def.key, def);
        }
    }
}
