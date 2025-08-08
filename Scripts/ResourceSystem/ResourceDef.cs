using JG.GameContent;
using System;
using UnityEngine;

[Serializable]
[ContentFolder("Resources")]
[CreateAssetMenu(menuName = "Defs/Resource")]
public class ResourceDef : ContentDef
{
    public string displayName;
    public string IconKey;
    [AssetFromFile("Resources/Icons", ".png", fileNameKey: nameof(IconKey))]
    public Sprite icon;

    public ResourceDef(string id, string displayName, Sprite icon = null) : base()
    {
        base.Id = id;
        this.displayName = displayName;
        this.icon = icon;
    }
}
