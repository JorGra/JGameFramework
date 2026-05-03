using System;
using JG.GameContent;
using UnityEngine;

[Serializable]
public class ResourceDropEntry
{
    [IdReference(typeof(ResourceDef))]
    public string ResourceId;

    [Min(0)] public int MinAmount = 1;
    [Min(0)] public int MaxAmount = 1;

    [Range(0f, 1f)]
    [Tooltip("Probability the drop occurs (0 = never, 1 = always).")]
    public float Chance = 1f;

    public int Roll()
    {
        if (string.IsNullOrEmpty(ResourceId)) return 0;
        if (Chance < 1f && UnityEngine.Random.value > Chance) return 0;
        int min = Mathf.Max(0, MinAmount);
        int max = Mathf.Max(min, MaxAmount);
        return UnityEngine.Random.Range(min, max + 1);
    }
}
