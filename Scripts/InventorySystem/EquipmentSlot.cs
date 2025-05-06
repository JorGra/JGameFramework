using System.Collections.Generic;
using JG.Inventory;

namespace JG.Inventory
{
    /// <summary>
    /// Stores an equipped item, gated by accepted tags.
    /// </summary>
    public class EquipmentSlot
    {
        private readonly HashSet<string> acceptedTags;

        public EquipmentSlot(IEnumerable<string> tags)
        {
            acceptedTags = new HashSet<string>(tags);
        }

        public ItemStack Equipped { get; private set; }

        public bool CanEquip(ItemData item)
        {
            foreach (string tag in item.EquipTags)
                if (acceptedTags.Contains(tag)) return true;
            return false;
        }

        public bool Equip(ItemStack stack, InventoryContext ctx)
        {
            if (!CanEquip(stack.Data)) return false;

            if (Equipped != null)
                Unequip(ctx);

            Equipped = stack;

            foreach (var def in stack.Data.Effects)
            {
                IItemEffect fx = ItemEffectFactory.Build(def);
                fx?.Apply(ctx);
            }
            return true;
        }

        public void Unequip(InventoryContext ctx)
        {
            if (Equipped == null) return;

            foreach (var def in Equipped.Data.Effects)
            {
                IItemEffect fx = ItemEffectFactory.Build(def);
                fx?.Remove(ctx);
            }
            Equipped = null;
        }
    }
}
