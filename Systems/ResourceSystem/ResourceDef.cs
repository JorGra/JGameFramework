using JG.Audio;
using JG.GameContent;
using System;
using UnityEngine;

[Serializable]
[ContentFolder("Resources")]
[CreateAssetMenu(menuName = "Defs/Resource")]
public class ResourceDef : ContentDef
{
    [Translatable]
    public string displayName;
    public string IconKey; // now holds a full path key (e.g., "Resources:Icons/Wood.png" or "/Icons/Wood.png")
    [AssetFromPath(nameof(IconKey))]
    public Sprite icon;

    [Tooltip("Played at the collector's position each time a particle of this resource is absorbed. Set frequent=true to throttle rapid bursts.")]
    public SoundData collectSound;

    public ResourceDef(string id, string displayName, Sprite icon = null) : base()
    {
        base.Id = id;
        this.displayName = displayName;
        this.icon = icon;
    }
}
