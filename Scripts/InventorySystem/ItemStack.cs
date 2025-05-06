using JG.Inventory;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Runtime instance of an item + current stack size.
    /// </summary>
    public class ItemStack
    {
        public ItemData Data { get; }
        public int Count { get; private set; }

        public ItemStack(ItemData data, int count)
        {
            Data = data;
            Count = Mathf.Clamp(count, 1, data.MaxStack);
        }

        public int Add(int amount)
        {
            int space = Data.MaxStack - Count;
            int accepted = Mathf.Min(space, amount);
            Count += accepted;
            return accepted;
        }

        public int Remove(int amount)
        {
            int removed = Mathf.Min(amount, Count);
            Count -= removed;
            return removed;
        }

        public bool IsFull => Count >= Data.MaxStack;
        public bool IsEmpty => Count <= 0;
    }
}
