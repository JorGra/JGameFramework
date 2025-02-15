using UnityEngine;

/// <summary>
/// A global stat definition. Each stat is defined as an asset with a unique numeric ID,
/// a display name, and a default value.
/// </summary>
[CreateAssetMenu(menuName = "Gameplay/Stats/Stat Definition", fileName = "NewStatDefinition")]
public class StatDefinition : ScriptableObject
{
    [Tooltip("Unique numeric identifier for this stat. Must be unique across all stat definitions.")]
    public int id;

    [Tooltip("Display name for the stat.")]
    public string statName;

    [Tooltip("Default value for the stat.")]
    public float defaultValue;
}