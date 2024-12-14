using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatsMediator
{
    readonly LinkedList<StatModifier> modifiers = new LinkedList<StatModifier>();
    public EventHandler<Query> Queries;

    public void PerfromQuery(object sender, Query query) => Queries?.Invoke(sender, query);

    public void AddModifier(StatModifier modifier)
    {
        modifiers.AddLast(modifier);
        Queries += modifier.Handle;

        modifier.OnDisposed += _ =>
        {
            modifiers.Remove(modifier);
            Queries -= modifier.Handle;
        };
    }

    public void Update(float deltaTime)
    {
        //Update all modifiers with deltaTime
        var node = modifiers.First;
        while (node != null)
        {
            var modifier = node.Value;
            modifier.Update(deltaTime);
            node = node.Next;
        }

        //Dispose all modifiers that are marked for removal
        node = modifiers.First;
        while (node != null)
        {
            var nextNode = node.Next;

            if (node.Value.MarkedForRemoval)
            {
                modifiers.Remove(node);
                node.Value.Dispose();
            }

            node = node.Next;
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