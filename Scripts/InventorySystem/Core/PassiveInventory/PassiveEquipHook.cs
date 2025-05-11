using System.Collections.Generic;

namespace JG.Inventory
{
    /// <summary>
    /// Applies equip effects when items enter the passive bag and reverses
    /// them when they leave. Quantity-aware & duplicate-safe.
    /// </summary>
    public class PassiveEquipHook : IInventoryHook
    {
        readonly Dictionary<string, List<IItemEffect>> active = new();

        public void OnChanged(ItemData data, int qty, InventoryContext ctx)
        {
            if (data == null || qty == 0 || ctx?.TargetStats == null) return;

            if (qty > 0) Apply(data, qty, ctx);
            else Remove(data, -qty, ctx);
        }

        /* ───────── helpers ───────── */

        void Apply(ItemData data, int qty, InventoryContext ctx)
        {
            if (!active.TryGetValue(data.Id, out var list))
                active[data.Id] = list = new List<IItemEffect>();

            for (int i = 0; i < qty; i++)
                foreach (var def in data.Effects)
                {
                    var fx = ItemEffectRegistry.Build(def.effectType, def.effectParams);
                    if (fx == null) continue;
                    fx.Apply(ctx);
                    list.Add(fx);
                }
        }

        void Remove(ItemData data, int qty, InventoryContext ctx)
        {
            if (!active.TryGetValue(data.Id, out var list) || list.Count == 0) return;

            /* pop & remove only as many as required */
            for (int i = 0; i < qty && list.Count > 0; i++)
            {
                list[0].Remove(ctx);
                list.RemoveAt(0);
            }

            if (list.Count == 0) active.Remove(data.Id);
        }
    }
}
