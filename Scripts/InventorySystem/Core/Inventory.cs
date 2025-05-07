using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>Ordered, dynamically growing inventory – each element is a stack.</summary>
    public class Inventory
    {
        public event System.Action Changed;                    // NEW

        private readonly List<InventorySlot> slots = new();
        public IReadOnlyList<InventorySlot> Slots => slots;

        public bool AddItem(ItemData item, int amount = 1)
        {
            foreach (var slot in slots)                        // merge first
                if (slot.CanMerge(item))
                    if (slot.TryAdd(item, amount))
                    { Changed?.Invoke(); return true; }

            var newSlot = new InventorySlot();                 // new stack
            if (newSlot.TryAdd(item, amount))
            {
                slots.Add(newSlot);
                Changed?.Invoke();                             // notify UI
                return true;
            }
            return false;
        }

        public bool RemoveItem(string itemId, int amount = 1)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty || slot.Stack.Data.Id != itemId) continue;

                int removed = slot.Remove(amount);
                if (slot.IsEmpty) slots.RemoveAt(i);
                if (removed > 0) { Changed?.Invoke(); return true; }
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
    }
}
