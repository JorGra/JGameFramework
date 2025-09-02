using JG.GameContent;
using System;
using UnityEngine;

[Serializable]
[ContentFolder("Resources")]
[CreateAssetMenu(menuName = "Defs/Resource")]
public class ResourceDef : ContentDef
{
    public string displayName;
    public string IconKey; // now holds a full path key (e.g., "Resources:Icons/Wood.png" or "/Icons/Wood.png")
    [AssetFromPath(nameof(IconKey))]
    public Sprite icon;

    public ResourceDef(string id, string displayName, Sprite icon = null) : base()
    {
        base.Id = id;
        this.displayName = displayName;
        this.icon = icon;
    }
}
