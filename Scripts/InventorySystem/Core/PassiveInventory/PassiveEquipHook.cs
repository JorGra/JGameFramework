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

        public void OnChanged(IInventoryItem data, int qty, IInventoryContext ctx)
        {
            if (data == null || qty == 0) return;
            if (!ctx.TryGet<Stats>(out var _)) return; // require Stats capability

            if (qty > 0) Apply(data, qty, ctx);
            else Remove(data, -qty, ctx);
        }

        /* ───────── helpers ───────── */

        void Apply(IInventoryItem data, int qty, IInventoryContext ctx)
        {
            if (data.Effects == null) return;

            if (!active.TryGetValue(data.Id, out var list))
                active[data.Id] = list = new List<IItemEffect>();


            for (int i = 0; i < qty; i++)
            {
                foreach (var def in data.Effects)
                {
                    var fx = def?.BuildEffect();
                    if (fx == null) continue;
                    fx.Apply(ctx);
                    list.Add(fx);
                }
            }
        }

        void Remove(IInventoryItem data, int qty, IInventoryContext ctx)
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
