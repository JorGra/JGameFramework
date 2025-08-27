// StatDefinition.cs
using UnityEngine;

public interface IStatDefinition
{
    string Key { get; }
    string StatName { get; }
    float DefaultValue { get; }
    Sprite Icon { get; }
}

[CreateAssetMenu(menuName = "Gameplay/Stats/Stat Definition", fileName = "NewStatDefinition")]
public class StatDefinition : ScriptableObject, IStatDefinition
{
    [Tooltip("Unique key for this stat. e.g. \"com.mygame.maxhealth\"")]
    public string key;

    [Tooltip("Display name for the stat.")]
    public string statName;

    [Tooltip("Default value for the stat.")]
    public float defaultValue;

    public Sprite icon;

    public string Key => key;

    public string StatName => statName;

    public float DefaultValue => defaultValue;

    public Sprite Icon => icon;
}
