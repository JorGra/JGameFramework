using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>Stores an equipped item, gated by accepted tags.</summary>
    public class EquipmentSlot
    {
        public event System.Action Changed;                    // NEW

        private readonly HashSet<string> acceptedTags;

        public EquipmentSlot(IEnumerable<string> tags) =>
            acceptedTags = new HashSet<string>(tags);

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

            if (Equipped != null) Unequip(ctx);                // drop previous
            Equipped = stack;

            //foreach (var d in stack.Data.Effects)
            //    ItemEffectRegistry.Build(d.effectType, d.effectParams)?.Apply(ctx);

            Changed?.Invoke();                                 // notify UI
            return true;
        }

        public void Unequip(InventoryContext ctx)
        {
            if (Equipped == null) return;
            foreach (var d in Equipped.Data.Effects)
                ItemEffectRegistry.Build(d.effectType, d.effectParams)?.Remove(ctx);

            Debug.Log("unequipped");
            Equipped = null;
            Changed?.Invoke();                                 // notify UI
        }
    }
}
