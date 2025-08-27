using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class StatsMediator
{
    readonly List<StatModifier> listModifiers = new List<StatModifier>();
    readonly Dictionary<IStatDefinition, IEnumerable<StatModifier>> modifierCache = new();
    IStatModifierApplicationOrder order = new NormalStatModifierApplicationOrder();

    public void PerfromQuery(object sender, Query query)
    {
        if (!modifierCache.ContainsKey(query.StatDefinition))
        {
            modifierCache[query.StatDefinition] = listModifiers
                .Where(x => (object)x.StatDefinition == query.StatDefinition)
                .ToList();
        }
        query.Value = order.Apply(modifierCache[query.StatDefinition], query.Value);
    }

    void InvalidateCache(IStatDefinition statDefinition)
    {
        modifierCache.Remove(statDefinition);
    }

    public void AddModifier(StatModifier modifier)
    {
        listModifiers.Add(modifier);
        InvalidateCache(modifier.StatDefinition);
        modifier.MarkedForRemoval = false;

        modifier.OnDispose += _ => InvalidateCache(modifier.StatDefinition);
        modifier.OnDispose += _ => listModifiers.Remove(modifier);
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (listModifiers.Contains(modifier))
        {
            modifier.MarkedForRemoval = true;
        }
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
    public readonly IStatDefinition StatDefinition;
    public float Value;

    public Query(IStatDefinition statDefinition, float value)
    {
        StatDefinition = statDefinition;
        Value = value;
    }
}