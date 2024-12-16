using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    MaxHealth,
}


public class Stats
{
    readonly StatsMediator mediator;
    readonly BaseStats baseStats;

    public StatsMediator Mediator => mediator;

    public float MaxHealth
    {
        get
        {

            var q = new Query(StatType.MaxHealth, baseStats.MaxHealth);
            mediator.PerfromQuery(this, q);
            return q.Value;
        }
    }

    public Stats(StatsMediator mediator, BaseStats baseStats)
    {
        this.baseStats = baseStats;
        this.mediator = mediator;
    }

    public override string ToString() => $"MaxHealth: {MaxHealth}";
}
