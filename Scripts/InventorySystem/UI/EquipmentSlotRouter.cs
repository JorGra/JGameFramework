using UnityEngine;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Connects the player equipment slots with the inventory.
    /// Manages the moving of the items between the inventory and the equipment slot
    /// </summary>
    public class EquipmentSlotRouter : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] InventoryComponent inventory;
        [SerializeField] Transform slotContainer;

        EquipmentSlotComponent[] slots;
        IStatsProvider provider;        

        void Awake()
        {
            /* slot + inventory discovery identical to before … */
            slots = slotContainer != null
                ? slotContainer.GetComponentsInChildren<EquipmentSlotComponent>(true)
                : GetComponentsInChildren<EquipmentSlotComponent>(true);

            if (inventory == null)
                inventory = GetComponent<InventoryComponent>() ??
                             GetComponentInParent<InventoryComponent>();

            /* one-time provider lookup (could also do per-call if hot-swapping) */
            provider = GetComponentInParent<IStatsProvider>();
        }

        /* ---------- public API ---------- */

        public bool Equip(ItemStack stack) => TryEquip(stack);
        public bool Unequip(EquipmentSlotComponent slot) =>
            slot != null && slot.Slot.Unequip(inventory.Runtime, NewCtx());
        public bool Use(ItemStack stack) =>
            inventory.Runtime.UseItem(stack.Data.Id, NewCtx());

        /* ---------- helpers ---------- */

        bool TryEquip(ItemStack stack)
        {
            var ctx = NewCtx();
            foreach (var s in slots)
            {
                if (!s.Slot.CanEquip(stack.Data)) continue;
                return s.Slot.Equip(stack, inventory.Runtime, ctx);
            }
            return false;
        }

        InventoryContext NewCtx() => new()
        {
            TargetStats = provider?.Stats      // may be null for stat-less NPC
        };
    }
}
