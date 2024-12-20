using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StatsDecorator : ScriptableObject
{
    public abstract float? GetStatValue(StatType statType);
}