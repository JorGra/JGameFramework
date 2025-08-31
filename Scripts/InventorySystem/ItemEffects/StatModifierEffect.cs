using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

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

        /* -------- factory called by registry -------- */
        public static IItemEffect FromJson(JToken token)
        {
            // Accept either array at root or object with 'entries'
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return new StatModifierEffect(new List<StatModifier>());

            if (token.Type == JTokenType.Array)
            {
                var pArr = new Params { entries = token.ToObject<Entry[]>() };
                return BuildFromParams(pArr);
            }
            else
            {
                var pObj = token.ToObject<Params>();
                return BuildFromParams(pObj);
            }
        }

        static IItemEffect BuildFromParams(Params data)
        {
            var list = new List<StatModifier>();
            if (data.entries != null)
            {
                var registry = StatRegistryProvider.Instance?.Registry; // may be null
                foreach (var e in data.entries)
                {
                    // validate key
                    var statKey = e.stat;
                    if (string.IsNullOrWhiteSpace(statKey))
                    {
                        Debug.LogWarning("[StatModifierEffect] Skipping entry with empty 'stat' key.");
                        continue;
                    }

                    // If registry available, warn+skip unknown keys
                    string keyToUse = statKey;
                    if (registry != null)
                    {
                        if (!registry.TryGet(statKey, out var def))
                        {
                            Debug.LogWarning($"[StatModifierEffect] Unknown stat key '{statKey}' – skipping.");
                            continue;
                        }
                        keyToUse = def.Key; // canonicalize casing
                    }

                    IOperationStrategy op = e.op switch
                    {
                        OperatorType.Add => new AddOperation(e.value),
                        OperatorType.Multiply => new MultiplyOperation(e.value),
                        OperatorType.Percentage => new PercentageOperation(e.value),
                        _ => new AddOperation(e.value)
                    };
                    var mod = new StatModifier(keyToUse, op, e.duration);
                    list.Add(mod);
                }
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
