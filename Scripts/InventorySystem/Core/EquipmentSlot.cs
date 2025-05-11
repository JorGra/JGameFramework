using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace JG.Inventory
{
    /// <summary>
    /// Holds one equipped item, validates tags, and moves stacks in/out of an
    /// <see cref="Inventory"/>.  Raises global events for other systems.
    /// </summary>
    public class EquipmentSlot
    {
        public event System.Action Changed;

        private readonly HashSet<string> acceptedTags;
        public ItemStack Equipped { get; private set; }

        public EquipmentSlot(IEnumerable<string> tags) =>
            acceptedTags = new HashSet<string>(tags);

        /// <returns>True if the item owns at least one accepted tag.</returns>
        public bool CanEquip(ItemData data) =>
            data != null &&
            (acceptedTags.Count == 0 || data.EquipTags.Any(acceptedTags.Contains));

        public bool Equip(ItemStack stack,
                          Inventory inventory,
                          InventoryContext ctx = null)
        {
            if (stack == null || !CanEquip(stack.Data)) return false;
            if (inventory != null &&
                !inventory.RemoveItem(stack.Data.Id, 1))       // pop 1 from inv
                return false;

            // hand back any previously equipped item
            if (Equipped != null && inventory != null)
                inventory.AddItem(Equipped.Data, Equipped.Count);

            Equipped = new ItemStack(stack.Data, 1);

            EventBus<ItemEquippedEvent>
                .Raise(new ItemEquippedEvent(this, Equipped, ctx));

            Changed?.Invoke();
            return true;
        }

        public bool Unequip(Inventory inventory, InventoryContext ctx = null)
        {
            if (Equipped == null) return false;

            if (inventory != null)
                inventory.AddItem(Equipped.Data, Equipped.Count);

            EventBus<ItemUnequippedEvent>
                .Raise(new ItemUnequippedEvent(this, Equipped, ctx));

            Equipped = null;
            Changed?.Invoke();
            return true;
        }
    }
}

namespace JG.Inventory
{
    /// <summary>
    /// Raised globally when any <see cref="EquipmentSlot"/> equips an item.
    /// </summary>
    public class ItemEquippedEvent : IEvent
    {
        public EquipmentSlot Slot { get; }
        public ItemStack Stack { get; }
        public InventoryContext Context { get; }

        public ItemEquippedEvent(EquipmentSlot slot,
                                 ItemStack stack,
                                 InventoryContext context)
        {
            Slot = slot;
            Stack = stack;
            Context = context;
        }
    }
}

namespace JG.Inventory
{
    /// <summary>
    /// Raised globally when an item is removed from an <see cref="EquipmentSlot"/>.
    /// </summary>
    public class ItemUnequippedEvent : IEvent
    {
        public EquipmentSlot Slot { get; }
        public ItemStack Stack { get; }
        public InventoryContext Context { get; }

        public ItemUnequippedEvent(EquipmentSlot slot,
                                   ItemStack stack,
                                   InventoryContext context)
        {
            Slot = slot;
            Stack = stack;
            Context = context;
        }
    }
}
