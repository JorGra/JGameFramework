using JG.GameContent;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>Ordered, dynamically growing inventory  each element is a stack.</summary>
    public class Inventory
    {
        public event System.Action Changed;

        readonly List<InventorySlot> slots = new();
        readonly IInventoryHook hook;         // NEW
        public readonly Func<IInventoryContext> ctxFactory;

        public IReadOnlyList<InventorySlot> Slots => slots;

        public Inventory(Func<IInventoryContext> contextFactory = null, IInventoryHook hk = null)
        {
            ctxFactory = contextFactory;
            hook = hk;
        }


        public bool AddItem(IInventoryItem item, int amount = 1)
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
                IInventoryItem capturedData = slot.Stack.Data;

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


        public bool UseItem(string itemId, IInventoryContext ctx)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Stack.Data.Id != itemId) continue;

                if (slot.Stack.Data.Effects != null)
                {
                    foreach (var def in slot.Stack.Data.Effects)
                        def?.BuildEffect()?.Apply(ctx);
                }
                else if (slot.Stack.Data.LegacyEffects != null)
                {
                    foreach (var legacy in slot.Stack.Data.LegacyEffects)
                        ItemEffectRegistry.Build(legacy.effectType, legacy.effectParams)?.Apply(ctx);
                }

                RemoveItem(itemId, 1);                         // fires Changed
                return true;
            }
            return false;
        }

        /// <summary>How many of <paramref name="itemId"/> are currently stored?</summary>
        public int CountItem(string itemId)
        {
            int total = 0;
            foreach (var s in slots)
                if (!s.IsEmpty && s.Stack.Data.Id == itemId)
                    total += s.Stack.Count;
            return total;
        }

        /// <summary>True when at least <paramref name="amount"/> copies are present.</summary>
        public bool HasItem(string itemId, int amount = 1) => CountItem(itemId) >= amount;

        void FireHook(IInventoryItem data, int qtySign)
        {
            if (hook == null || data == null || qtySign == 0) return;
            var ctx = ctxFactory?.Invoke();
            hook.OnChanged(data, qtySign, ctx);
        }

        bool TryMergeExisting(IInventoryItem item, int amount)
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
