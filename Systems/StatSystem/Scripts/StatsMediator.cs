using System;
using System.Collections.Generic;
using System.Linq;

public class StatsMediator
{
    private readonly List<StatModifier> listModifiers = new();
    private readonly Dictionary<string, List<StatModifier>> modifierCache =
        new(StringComparer.OrdinalIgnoreCase);

    private IStatModifierApplicationOrder order = new NormalStatModifierApplicationOrder();

    public void PerfromQuery(object sender, Query query)
    {
        if (query == null || string.IsNullOrWhiteSpace(query.StatKey))
            return;

        if (!modifierCache.TryGetValue(query.StatKey, out var cached))
        {
            cached = listModifiers
                .Where(x => string.Equals(x.StatKey, query.StatKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            modifierCache[query.StatKey] = cached;
        }

        query.Value = order.Apply(cached, query.Value);
    }

    private void InvalidateCache(string statKey)
    {
        if (!string.IsNullOrWhiteSpace(statKey))
            modifierCache.Remove(statKey);
    }

    public void AddModifier(StatModifier modifier)
    {
        if (modifier == null) return;

        listModifiers.Add(modifier);
        InvalidateCache(modifier.StatKey);
        modifier.MarkedForRemoval = false;

        modifier.OnDispose += _ => InvalidateCache(modifier.StatKey);
        modifier.OnDispose += _ => listModifiers.Remove(modifier);
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (modifier != null && listModifiers.Contains(modifier))
            modifier.MarkedForRemoval = true;
    }

    public void Update(float deltaTime)
    {
        foreach (var modifier in listModifiers)
            modifier.Update(deltaTime);

        foreach (var modifier in listModifiers.Where(x => x.MarkedForRemoval).ToList())
            modifier.Dispose();
    }
}

public class Query
{
    public readonly string StatKey;
    public float Value;

    public Query(string statKey, float value)
    {
        StatKey = statKey;
        Value = value;
    }
}
