using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class StatsMediator
{
    readonly List<StatModifier> listModifiers = new List<StatModifier>();

    public void PerfromQuery(object sender, Query query)
    {
        foreach (var modifier in listModifiers)
        {
            modifier.Handle(sender, query);
        }
    }
    public void AddModifier(StatModifier modifier)
    {
        listModifiers.Add(modifier);
        modifier.MarkedForRemoval = false;
        modifier.OnDispose += _ => listModifiers.Remove(modifier);
    }

    public void Update(float deltaTime)
    {

        foreach (var modifier in listModifiers)
        {
            modifier.Update(deltaTime);
        }
        foreach (var modifier in listModifiers.Where(x => x.MarkedForRemoval).ToList())
        {
            modifier.Dispose();
        }
    }

}

public class Query
{
    public readonly StatType StatType;
    public float Value;

    public Query(StatType statType, float value)
    {
        StatType = statType;
        Value = value;
    }
}