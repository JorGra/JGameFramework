using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Adds permanent (or timed) stat modifiers.
    /// Put this in item JSON with effect id <c>"StatMod"</c>.
    /// </summary>
    [ItemEffect("StatMod")]
    public class StatModifierEffect : IItemEffect
    {
        readonly List<StatModifier> modifiers = new();

        public StatModifierEffect(List<StatModifier> mods) => modifiers = mods;

        public void Apply(InventoryContext ctx)
        {
            if (ctx?.TargetStats == null) return;
            foreach (var m in modifiers)
                ctx.TargetStats.Mediator.AddModifier(m);
        }

        public void Remove(InventoryContext ctx)
        {
            if (ctx?.TargetStats == null) return;
            foreach (var m in modifiers)
                ctx.TargetStats.Mediator.RemoveModifier(m);
        }

        /* -------- factory called by registry -------- */

        public static IItemEffect FromJson(string json)
        {
            var data = JsonUtility.FromJson<Params>(json);
            var list = new List<StatModifier>();

            foreach (var e in data.entries)
            {
                var def = StatRegistryProvider.Instance.Registry.Get(e.stat);
                IOperationStrategy op;
                if (e.op == OperatorType.Add)
                {
                    op = new AddOperation(e.value);
                }
                else
                {
                    op = new MultiplyOperation(e.value);
                }

                var mod = new StatModifier(def, op, e.duration);
                list.Add(mod);
            }
            return new StatModifierEffect(list);
        }

        static StatModifierEffect() =>
            ItemEffectRegistry.Register<StatModifierEffect>(FromJson);

        /* ------------ DTO ------------ */

        [System.Serializable]
        struct Params
        {
            public Entry[] entries;
        }

        [System.Serializable]
        struct Entry
        {
            public string stat;
            public OperatorType op;
            public float value;
            public float duration;     // 0 = permanent (while equipped)
        }
    }
}
