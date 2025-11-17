using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using JG.GameContent;

namespace JG.Inventory
{
    /// <summary>
    /// Adds permanent (or timed) stat modifiers.
    /// Put this in item JSON with effect id <c>"StatMod"</c>.
    /// </summary>
    public class StatModifierEffect : IItemEffect
    {
        readonly List<StatModifier> modifiers = new();

        public StatModifierEffect(List<StatModifier> mods) => modifiers = mods;

        public void Apply(IInventoryContext ctx)
        {
            if (!ctx.TryGet<Stats>(out var stats)) return;
            foreach (var m in modifiers)
                stats.Mediator.AddModifier(m);

            Debug.Log($"[StatModifierEffect] Applied {modifiers.Count} modifiers");
        }

        public void Remove(IInventoryContext ctx)
        {
            if (!ctx.TryGet<Stats>(out var stats)) return;
            foreach (var m in modifiers) stats.Mediator.RemoveModifier(m);
        }

    }
}
