using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>Ordered, dynamically growing inventory – each element is a stack.</summary>
    public class Inventory
    {
        public event System.Action Changed;

        readonly List<InventorySlot> slots = new();
        readonly IInventoryHook hook;         // NEW
        readonly IStatsProvider statsProv;    // for ctx

        public IReadOnlyList<InventorySlot> Slots => slots;

        public Inventory(IStatsProvider prov = null, IInventoryHook hk = null)
        {
            statsProv = prov;
            hook = hk;
        }


        public bool AddItem(ItemData item, int amount = 1)
        {
            if (TryMergeExisting(item, amount))           // applies hook internally
                return true;

            var slot = new InventorySlot();
            if (!slot.TryAdd(item, amount)) return false;

            slots.Add(slot);
            FireHook(item, +amount);                      // NEW
            Changed?.Invoke();
            return true;
        }



        public bool RemoveItem(string itemId, int amount = 1)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Stack.Data.Id != itemId) continue;

                /* capture data BEFORE we modify the slot */
                ItemData capturedData = slot.Stack.Data;

                int removed = slot.Remove(amount);
                if (removed > 0)
                {
                    if (slot.IsEmpty) slots.RemoveAt(i);
                    FireHook(capturedData, -removed);      // use cached data
                    Changed?.Invoke();
                    return true;
                }
                return false;
            }
            return false;
        }


        public bool UseItem(string itemId, InventoryContext ctx)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Stack.Data.Id != itemId) continue;

                foreach (var def in slot.Stack.Data.Effects)
                    ItemEffectRegistry.Build(def.effectType, def.effectParams)?.Apply(ctx);

                RemoveItem(itemId, 1);                         // fires Changed
                return true;
            }
            return false;
        }


        void FireHook(ItemData data, int qtySign)
        {
            if (hook == null || data == null || qtySign == 0) return;
            var ctx = new InventoryContext { TargetStats = statsProv?.Stats };
            hook.OnChanged(data, qtySign, ctx);
        }

        bool TryMergeExisting(ItemData item, int amount)
        {
            foreach (var slot in slots)
                if (slot.CanMerge(item) && slot.TryAdd(item, amount))
                {
                    FireHook(item, +amount);          
                    Changed?.Invoke();
                    return true;
                }
            return false;
        }
    }
}
