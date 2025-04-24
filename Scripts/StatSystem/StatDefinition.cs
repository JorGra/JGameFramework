// StatDefinition.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Stats/Stat Definition", fileName = "NewStatDefinition")]
public class StatDefinition : ScriptableObject
{
    [Tooltip("Unique key for this stat. e.g. \"com.mygame.maxhealth\"")]
    public string key;

    [Tooltip("Display name for the stat.")]
    public string statName;

    [Tooltip("Default value for the stat.")]
    public float defaultValue;

    public Sprite icon;
}
