using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds a global list of stat definitions. This asset must be placed in a Resources folder
/// (and named "StatRegistry") so it can be loaded at runtime.
/// </summary>
[CreateAssetMenu(menuName = "Gameplay/Stats/Stat Registry", fileName = "NewStatRegistry")]
public class StatRegistry : ScriptableObject
{
    [Tooltip("List of all stat definitions available in the game.")]
    public List<StatDefinition> statDefinitions;

    private Dictionary<int, StatDefinition> lookupById;

    private void OnEnable()
    {
        lookupById = new Dictionary<int, StatDefinition>();

        foreach (var def in statDefinitions)
        {
            if (def != null)
            {
                if (!lookupById.ContainsKey(def.id))
                {
                    lookupById.Add(def.id, def);
                }
                else
                {
                    Debug.LogError($"Duplicate StatDefinition id {def.id} found in StatRegistry.");
                }
            }
        }
    }

    /// <summary>
    /// Retrieves a stat definition using its unique ID.
    /// </summary>
    public StatDefinition GetStatDefinitionById(int id)
    {
        if (lookupById != null && lookupById.TryGetValue(id, out var def))
        {
            return def;
        }
        throw new KeyNotFoundException($"StatDefinition with id {id} not found in the registry.");
    }
}
