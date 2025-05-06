
namespace JG.Inventory
{
    /// <summary>
    /// Dynamic list of <see cref="InventorySlot"/>s – no hard limit unless imposed externally.
    /// </summary>
    public class Inventory : Container<int, InventorySlot>
    {
        public Inventory()
        {
            // Slot 0 exists by default for convenience.
            entries[0] = new InventorySlot();
        }

        public bool AddItem(ItemData item, int amount = 1)
        {
            // 1. First try to merge into existing stack.
            foreach (InventorySlot slot in entries.Values)
            {
                if (slot.CanMerge(item))
                    if (slot.TryAdd(item, amount)) return true;
            }

            // 2. Otherwise create new slot.
            int newKey = entries.Count;
            var newSlot = new InventorySlot();
            if (newSlot.TryAdd(item, amount))
            {
                entries[newKey] = newSlot;
                return true;
            }

            return false;
        }

        public bool RemoveItem(string itemId, int amount = 1)
        {
            foreach ((int key, InventorySlot slot) in entries)
            {
                if (slot.IsEmpty || slot.Stack.Data.Id != itemId) continue;
                int removed = slot.Remove(amount);
                if (slot.IsEmpty) entries.Remove(key);
                return removed > 0;
            }
            return false;
        }

        public bool UseItem(string itemId, InventoryContext ctx)
        {
            foreach ((int key, InventorySlot slot) in entries)
            {
                if (slot.IsEmpty || slot.Stack.Data.Id != itemId) continue;

                // Run effects
                foreach (var effectDef in slot.Stack.Data.Effects)
                {
                    IItemEffect fx = ItemEffectFactory.Build(effectDef);
                    fx?.Apply(ctx);
                }

                RemoveItem(itemId, 1);
                // Raise event etc. (omitted for brevity)
                return true;
            }
            return false;
        }
    }
}
