using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Brotato-style weapon handler:
    /// - Only equips into EMPTY weapon slots.
    /// - Never touches the bag (passes inventory: null).
    /// - Fails to add if no free slot is available.
    /// </summary>
    public class WeaponSlotRouter : MonoBehaviour
    {
        [Header("Discovery")]
        [SerializeField] private Transform slotContainer;

        [Header("Classification")]
        [Tooltip("At least ONE of these tags must exist on the item to treat it as a weapon.")]
        [SerializeField] private List<string> weaponTags = new() { "Weapon" };

        private EquipmentSlotComponent[] slots;
        private IStatsProvider provider;

        void Awake()
        {
            // find weapon slots under the given container (or under this object)
            slots = slotContainer != null
                ? slotContainer.GetComponentsInChildren<EquipmentSlotComponent>(true)
                : GetComponentsInChildren<EquipmentSlotComponent>(true);

            // cache where to pull the Target Stats for InventoryContext
            provider = GetComponentInParent<IStatsProvider>();
        }

        /* ---------- public API ---------- */

        /// <summary>True if the item is *tagged* as a weapon.</summary>
        public bool IsWeapon(IInventoryItem data) =>
            data != null &&
            data.EquipTags != null &&
            data.EquipTags.Any(weaponTags.Contains);    // uses item.EquipTags from your IInventoryItem. :contentReference[oaicite:6]{index=6}

        /// <summary>True if there is a FREE compatible weapon slot.</summary>
        public bool HasFreeSlotFor(IInventoryItem data)
        {
            if (!IsWeapon(data)) return false;

            foreach (var s in slots)
                if (s.Slot.Equipped == null && s.Slot.CanEquip(data))
                    return true;
            return false;
        }

        /// <summary>
        /// Add a weapon to an EMPTY slot. No slot available → returns false (no pickup).
        /// </summary>
        public bool TryAddWeapon(IInventoryItem data)
        {
            if (!IsWeapon(data)) return false;

            foreach (var s in slots)
            {
                if (s.Slot.Equipped != null) continue;            // critical: never overwrite
                if (!s.Slot.CanEquip(data)) continue;

                // Equip with 'inventory: null' so nothing is moved in/out of the bag.
                // This still raises ItemEquippedEvent → EquipmentEffectController applies effects. :contentReference[oaicite:7]{index=7} :contentReference[oaicite:8]{index=8}
                return s.Slot.Equip(new ItemStack(data, 1), inventory: null, ctx: NewCtx());
            }
            return false;
        }

        public bool TryAddWeapon(ItemStack stack) =>
            stack != null && TryAddWeapon(stack.Data);

        /// <summary>
        /// Explicit unequip that also doesn't send the item back to the bag.
        /// Useful if you want to "drop/sell" weapons on purpose.
        /// </summary>
        public bool UnequipToNowhere(EquipmentSlotComponent slot)
        {
            if (slot == null || slot.Slot.Equipped == null) return false;
            // Unequip with 'inventory: null' → raises ItemUnequippedEvent & clears slot. :contentReference[oaicite:9]{index=9}
            return slot.Slot.Unequip(inventory: null, ctx: NewCtx());
        }

        /* ---------- helpers ---------- */

        InventoryContext NewCtx() => new()
        {
            TargetStats = provider?.Stats   // matches EquipmentSlotRouter's context creation. :contentReference[oaicite:10]{index=10} :contentReference[oaicite:11]{index=11}
        };
    }
}
