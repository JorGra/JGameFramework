using UnityEngine;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Routes items between an <see cref="Inventory"/> and the player’s equipment slots.
    /// </summary>
    public class PlayerEquipmentBridge : MonoBehaviour
    {
        [SerializeField] private Transform slotContainer;
        [SerializeField] private EquipmentSlotComponent[] slots;
        [SerializeField] private InventoryComponent inventory;

        bool awakeDone;

        void Awake()
        {
            if (slots.Length == 0)
                slots = slotContainer.GetComponentsInChildren<EquipmentSlotComponent>(true);

            if (inventory == null)
                inventory = GetComponentInParent<InventoryComponent>();

            awakeDone = true;
        }

        /// <summary>
        /// Equips a single copy of <paramref name="stack"/> into the first compatible slot.
        /// Removes that copy from the inventory beforehand to avoid duplication.
        /// </summary>
        /// <remarks>
        /// If equipping fails the item is immediately added back so state stays consistent.
        /// </remarks>
        public bool Equip(ItemStack stack)
        {
            EnsureAwake();
            if (stack == null || stack.Data == null) return false;

            /* try every slot until one accepts the item */
            foreach (var s in slots)
            {
                if (!s.Slot.CanEquip(stack.Data)) continue;

                /* take ONE item from the inventory (or the whole stack if non-stackable) */
                int quantity = 1;                                   // change if you support multi-equip
                if (!inventory.Runtime.RemoveItem(stack.Data.Id, quantity))
                    return false;                                   // nothing to take

                var equipStack = new ItemStack(stack.Data, quantity);
                bool equipped = s.Slot.Equip(equipStack, new InventoryContext());

                if (!equipped)                                     // roll back if slot refused
                    inventory.Runtime.AddItem(stack.Data, quantity);

                return equipped;
            }
            return false;                                          // no compatible slot found
        }

        /// <summary>Transfers an equipped item back into the inventory.</summary>
        public bool Unequip(EquipmentSlotComponent slot)
        {
            EnsureAwake();

            var st = slot.Slot.Equipped;
            if (st == null) return false;

            if (!inventory.Runtime.AddItem(st.Data, st.Count)) return false;

            slot.Slot.Unequip(new InventoryContext());
            return true;
        }

        void EnsureAwake() { if (!awakeDone) Awake(); }
    }
}
