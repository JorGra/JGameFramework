using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// List of all stats
/// </summary>
public enum StatType
{
    MaxHealth,
    Damage,
    AttackSpeed,
    MoveSpeed,
    Range
}


public class Stats
{
    readonly StatsMediator mediator;
    readonly List<StatsDecorator> decorators;

    public StatsMediator Mediator => mediator;

    public Stats(StatsMediator mediator, IEnumerable<StatsDecorator> decorators = null)
    {
        this.mediator = mediator;
        this.decorators = decorators != null ? decorators.ToList() : new List<StatsDecorator>();
    }

    public float GetStat(StatType statType, float baseValue = 0)
    {
        // start with base value, then add decorators
        float value = baseValue;
        foreach (var deco in decorators)
        {
            var decoValue = deco.GetStatValue(statType);
            if (decoValue.HasValue)
                value += decoValue.Value;
        }

        // Add Modifiers via mediator 
        var q = new Query(statType, value);
        mediator.PerfromQuery(this, q);
        return q.Value;
    }

}
