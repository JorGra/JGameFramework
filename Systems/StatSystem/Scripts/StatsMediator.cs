using System;
using System.Collections.Generic;

public class StatsMediator
{
    private readonly List<StatModifier> listModifiers = new();
    private readonly Dictionary<string, List<StatModifier>> modifierCache =
        new(StringComparer.OrdinalIgnoreCase);

    private IStatModifierApplicationOrder order = new NormalStatModifierApplicationOrder();

    public void PerfromQuery(object sender, ref Query query)
    {
        string statKey = query.StatKey;
        if (string.IsNullOrWhiteSpace(statKey))
            return;

        if (!modifierCache.TryGetValue(statKey, out var cached))
        {
            cached = new List<StatModifier>();
            for (int i = 0; i < listModifiers.Count; i++)
            {
                if (string.Equals(listModifiers[i].StatKey, statKey, StringComparison.OrdinalIgnoreCase))
                {
                    cached.Add(listModifiers[i]);
                }
            }
            modifierCache[statKey] = cached;
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
        for (int i = 0; i < listModifiers.Count; i++)
        {
            listModifiers[i].Update(deltaTime);
        }

        // Iterate backwards to safely remove while iterating
        for (int i = listModifiers.Count - 1; i >= 0; i--)
        {
            if (listModifiers[i].MarkedForRemoval)
            {
                listModifiers[i].Dispose();
            }
        }
    }
}

public struct Query
{
    public readonly string StatKey;
    public float Value;

    public Query(string statKey, float value)
    {
        StatKey = statKey;
        Value = value;
    }
}
