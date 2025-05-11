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
            if (TryMergeExisting(item, amount)) return true;  // unchanged

            var newSlot = new InventorySlot();
            if (!newSlot.TryAdd(item, amount)) return false;

            slots.Add(newSlot);
            FireHook(newSlot.Stack, added: true);
            Changed?.Invoke();
            return true;
        }


        public bool RemoveItem(string itemId, int amount = 1)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Stack.Data.Id != itemId) continue;

                /* capture before we possibly clear the slot */
                var capturedData = slot.Stack.Data;

                int removed = slot.Remove(amount);
                if (slot.IsEmpty) slots.RemoveAt(i);

                if (removed > 0)
                {
                    FireHook(new ItemStack(capturedData, removed), added: false);
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


        void FireHook(ItemStack s, bool added)
        {
            if (hook == null) return;
            var ctx = new InventoryContext { TargetStats = statsProv?.Stats };
            hook.OnChanged(s, ctx, added);
        }

        bool TryMergeExisting(ItemData item, int amount)
        {
            foreach (var slot in slots)
                if (slot.CanMerge(item) && slot.TryAdd(item, amount))
                {
                    FireHook(new ItemStack(item, amount), added: true);
                    Changed?.Invoke();
                    return true;
                }
            return false;
        }
    }
}
