using System;
using UnityEngine;

[Serializable]
public struct Resource
{
    public string id;      // Unique key for resource type
    public string displayName;
    public Sprite icon;

    public Resource(string id, string displayName, Sprite icon = null)
    {
        this.id = id;
        this.displayName = displayName;
        this.icon = icon;
    }
}
