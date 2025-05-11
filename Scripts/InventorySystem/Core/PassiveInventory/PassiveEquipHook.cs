using System.Collections.Generic;

namespace JG.Inventory
{
    /// <summary>
    /// Applies all equip effects when a stack is added and undoes them when
    /// removed. Stacks *fully*—adding 3 charms → 3× effects.
    /// </summary>
    public class PassiveEquipHook : IInventoryHook
    {
        readonly Dictionary<ItemStack, List<IItemEffect>> cache = new();

        public void OnChanged(ItemStack s, InventoryContext ctx, bool added)
        {
            if (added)
            {
                var list = new List<IItemEffect>();
                for (int i = 0; i < s.Count; i++)
                    foreach (var def in s.Data.Effects)
                    {
                        var fx = ItemEffectRegistry.Build(def.effectType, def.effectParams);
                        fx?.Apply(ctx);
                        if (fx != null) list.Add(fx);
                    }
                cache[s] = list;        // remember so we can undo
            }
            else if (cache.TryGetValue(s, out var lst))
            {
                foreach (var fx in lst) fx.Remove(ctx);
                cache.Remove(s);
            }
        }
    }
}
