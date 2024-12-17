using System.Collections;
using System.Collections.Generic;
using System.Linq;



public class StatsMediator
{
    readonly List<StatModifier> listModifiers = new List<StatModifier>();
    readonly Dictionary<StatType, IEnumerable<StatModifier>> modifierCache = new();
    IStatModifierApplicationOrder order = new NormalStatModifierApplicationOrder();


    public void PerfromQuery(object sender, Query query)
    {
        if (!modifierCache.ContainsKey(query.StatType))
        {
            modifierCache[query.StatType] = listModifiers.Where(x => x.StatType == query.StatType).ToList();
        }
        query.Value = order.Apply(modifierCache[query.StatType], query.Value);
    }

    void InvalidateCache(StatType statType)
    {
        modifierCache.Remove(statType);
    }

    public void AddModifier(StatModifier modifier)
    {
        listModifiers.Add(modifier);
        InvalidateCache(modifier.StatType);
        modifier.MarkedForRemoval = false;

        modifier.OnDispose += _ => InvalidateCache(modifier.StatType);
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