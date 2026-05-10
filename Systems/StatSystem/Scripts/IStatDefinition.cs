using UnityEngine;

public interface IStatDefinition
{
    string Key { get; }
    string StatName { get; }
    float DefaultValue { get; }
    Sprite Icon { get; }
}
