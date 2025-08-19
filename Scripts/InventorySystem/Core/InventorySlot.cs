using JG.Inventory;

namespace JG.Inventory
{
    /// <summary>
    /// Holds a single <see cref="ItemStack"/> or is empty.
    /// </summary>
    public class InventorySlot
    {
        public ItemStack Stack { get; private set; }

        public bool IsEmpty => Stack == null;

        public bool CanMerge(IInventoryItem item) =>
            !IsEmpty && Stack.Data.Id == item.Id && !Stack.IsFull;

        public bool TryAdd(IInventoryItem item, int amount)
        {
            if (IsEmpty)
            {
                Stack = new ItemStack(item, amount);
                return true;
            }

            if (Stack.Data.Id != item.Id) return false;
            int accepted = Stack.Add(amount);
            return accepted > 0;
        }

        public int Remove(int amount)
        {
            if (IsEmpty) return 0;
            int removed = Stack.Remove(amount);
            if (Stack.IsEmpty) Stack = null;
            return removed;
        }
    }
}
