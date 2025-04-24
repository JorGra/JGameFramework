// StatRegistry.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds every <see cref="StatDefinition"/> in the game, supporting
/// static ScriptableObjects and dynamic JSON loading (including icons).
/// </summary>
[CreateAssetMenu(menuName = "Gameplay/Stats/Stat Registry",
                 fileName = "NewStatRegistry")]
public class StatRegistry : ScriptableObject
{
    [Tooltip("List of all stat definitions baked into the build.")]
    [SerializeField] private List<StatDefinition> statDefinitions = new();

    [Header("Runtime")]
    [Tooltip("Fallback sprite used when a JSON-specified icon cannot be found.")]
    [SerializeField] private Sprite defaultIcon;

    const string ICON_BASE_PATH = "StatDefinitions/Icons/";   // under Resources/

    Dictionary<string, StatDefinition> lookupByKey;

    #region JSON DTOs
    [Serializable]
    private class StatDefJson
    {
        public string key;
        public string statName;
        public float defaultValue;
        public string iconPath;      // NEW
    }

    [Serializable] private class StatDefListJson { public List<StatDefJson> stats; }
    #endregion

    /// <summary>
    /// Replace/merge dynamic stats by parsing <paramref name="jsonText"/>.
    /// Any ScriptableObject assets already in <c>statDefinitions</c> remain.
    /// </summary>
    public void InitializeFromJsonText(string jsonText)
    {
        /* 1) purge previous runtime-only SOs */
        statDefinitions.RemoveAll(d => d == null ||
            (d.hideFlags & HideFlags.HideAndDontSave) != 0);

        /* 2) rebuild lookup from remaining (asset) definitions */
        BuildLookupFromSO();

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            Debug.LogWarning("[StatRegistry] Empty JSON; no dynamic stats loaded.");
            return;
        }

        StatDefListJson wrapper;
        try
        {
            wrapper = JsonUtility.FromJson<StatDefListJson>(jsonText);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StatRegistry] JSON parse error: {e.Message}");
            return;
        }

        if (wrapper?.stats == null) return;

        /* 3) create transient SOs from DTOs */
        foreach (var dto in wrapper.stats)
        {
            if (string.IsNullOrWhiteSpace(dto.key))
            {
                Debug.LogWarning("[StatRegistry] JSON entry missing key; skipping.");
                continue;
            }

            if (lookupByKey.ContainsKey(dto.key))
            {
                Debug.LogWarning($"[StatRegistry] Duplicate key '{dto.key}'; skipping.");
                continue;
            }

            var so = ScriptableObject.CreateInstance<StatDefinition>();
            so.hideFlags = HideFlags.HideAndDontSave;
            so.key = dto.key;
            so.statName = dto.statName;
            so.defaultValue = dto.defaultValue;

            if (!string.IsNullOrWhiteSpace(dto.iconPath))
            {
                string trimmed = dto.iconPath.TrimStart('/', '\\');
                string resPath = ICON_BASE_PATH + trimmed;
                so.icon = Resources.Load<Sprite>(resPath);

                if (so.icon == null)
                {
                    Debug.LogWarning(
                        $"[StatRegistry] Sprite '{resPath}' not found for '{dto.key}'.");
                    so.icon = defaultIcon;
                }
            }

            statDefinitions.Add(so);
            lookupByKey.Add(so.key, so);
        }
    }

    /// <exception cref="KeyNotFoundException" />
    public StatDefinition Get(string key)
    {
        if (lookupByKey != null && lookupByKey.TryGetValue(key, out var def))
            return def;

        throw new KeyNotFoundException(
            $"StatDefinition with key '{key}' not found.");
    }

    public IReadOnlyList<StatDefinition> StatDefinitions => statDefinitions;

    /* ───────────────────────── helpers ───────────────────────── */

    void BuildLookupFromSO()
    {
        lookupByKey = new Dictionary<string, StatDefinition>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var def in statDefinitions)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.key)) continue;

            if (lookupByKey.ContainsKey(def.key))
            {
                Debug.LogError(
                    $"[StatRegistry] Duplicate key '{def.key}' in baked assets.");
                continue;
            }
            lookupByKey.Add(def.key, def);
        }
    }
}
