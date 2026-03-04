using JG.GameContent;
using UnityEngine;

public abstract class StatDef : ContentDef, IStatDefinition
{
    public string Key => Id;

    [field: SerializeField, Translatable] public string StatName { get; set; }

    [field: SerializeField] public float DefaultValue { get; set; }

    public string IconKey;
    [AssetFromPath(nameof(IconKey))]
    [field: SerializeField] public Sprite Icon { get; set; }
}
