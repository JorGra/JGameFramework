using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "LivingEntityStats", menuName = "Gameplay/Stats/LivingEntityStats")]
public class LivingEntityStatsDecorator : StatsDecorator
{
    public float MaxHealth = 3f;
    public override float? GetStatValue(StatType statType)
    {

        return statType switch
        {
            StatType.MaxHealth => MaxHealth,
            _ => null,
        };
    }
}
