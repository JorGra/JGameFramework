using JG.Inventory.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Single entry point for a player's inventory + equipment.
    /// - TryAddItem → Brotato-style intake (weapons: empty slot or fail; others → bag)
    /// - Equip/Unequip/Use → manual actions for UI/context
    /// Keeps tag-based slot validation and events exactly as-is.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EquipmentHub : MonoBehaviour
    {
        [Header("Optional explicit links (auto-discovered if null)")]
        [SerializeField] private InventoryComponent bag;
        public EquipmentUI EquipmentUI;

        [Header("Classification")]
        [Tooltip("At least ONE of these tags marks an item as a weapon.")]
        [SerializeField] private List<string> weaponTags = new() { "Weapon" };

        //private List<EquipmentUI.SlotBinding> slots = new();
        private IStatsProvider provider;

        public Inventory Inventory => bag != null ? bag.Runtime : null;

        void Awake()
        {
            // Find inventory on self/parents
            bag ??= GetComponent<InventoryComponent>() ?? GetComponentInParent<InventoryComponent>(true);

            // Find equipment Slots under provided container or under us
            //Init(EquipmentUI);

            // Cache stats provider for InventoryContext (effects)
            provider = GetComponentInParent<IStatsProvider>();
        }

        public void Init(EquipmentUI equipUI)
        {
            EquipmentUI = equipUI;
            EquipmentUI.Init();
        }

        /* -------------------- Public API -------------------- */

        /// <summary>
        /// Add items. Weapons → try auto-equip into EMPTY slot (no bag involvement).
        /// Non-weapons → bag. If a weapon can't be placed, returns false (pickup fails).
        /// </summary>
        public bool TryAddItem(IInventoryItem item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            for (int i = 0; i < quantity; i++)
            {
                if (IsWeapon(item))
                {
                    if (!TryAutoEquipWeapon(item))
                    {
                        Debug.Log($"[{name}] Failed to auto-equip weapon {item.Id}; no free slot.");
                        return false; // strict: fail if no free slot
                    }
                }
                else
                {
                    if (Inventory == null || !Inventory.AddItem(item, 1))
                    {
                        Debug.Log($"[{name}] Failed to add non-weapon item {item.Id} to bag.");
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Manual UI action: equip from the bag into the first compatible slot (replaces & returns previous to bag).</summary>
        public bool Equip(ItemStack stack)
        {
            if (stack == null || Inventory == null) return false;
            var ctx = NewCtx();

            foreach (var s in EquipmentUI.Slots)
            {
                if (!s.slotComponent.Slot.CanEquip(stack.Data)) continue;
                // EquipmentSlot.Equip pops 1 from inventory and puts previously equipped back into inventory, then raises events.
                return s.slotComponent.Slot.Equip(stack, Inventory, ctx);
            }
            return false;
        }

        /// <summary>Manual UI action: unequip to the bag.</summary>
        public bool Unequip(EquipmentSlotComponent slot) =>
            slot != null && Inventory != null && slot.Slot.Unequip(Inventory, NewCtx()); // raises events. 

        /// <summary>Manual UI action: use an item from the bag.</summary>
        public bool Use(ItemStack stack) =>
            stack != null && Inventory != null && Inventory.UseItem(stack.Data.Id, NewCtx()); // triggers effects. 

        /// <summary>Helper: true if there is an EMPTY compatible slot for this item.</summary>
        public bool HasFreeSlotFor(IInventoryItem data)
        {
            if (!IsWeapon(data)) return false;
            foreach (var s in EquipmentUI.Slots)
                if (s.slotComponent.Slot.Equipped == null && s.slotComponent.Slot.CanEquip(data))
                    return true;
            return false;
        }

        /// <summary>Helper: intentionally unequip without returning to the bag (e.g., drop/sell).</summary>
        public bool UnequipToVoid(EquipmentSlotComponent slot) =>
            slot != null && slot.Slot.Unequip(inventory: null, ctx: NewCtx()); // raises ItemUnequippedEvent. 

        /* -------------------- Internals -------------------- */

        bool IsWeapon(IInventoryItem data) =>
            data != null && data.EquipTags != null && data.EquipTags.Any(weaponTags.Contains); // same criteria you use today.

        bool TryAutoEquipWeapon(IInventoryItem data)
        {
            foreach (var s in EquipmentUI.Slots)
            {
                if (s.slotComponent.Slot.Equipped != null) continue;            // never overwrite
                if (!s.slotComponent.Slot.CanEquip(data)) continue;

                // Auto-equip to EMPTY slot without touching the bag; still raises ItemEquippedEvent. 
                Debug.Log($"[{name}] Auto-equipping weapon {data.Id} into slot {s.slotComponent.name}.");
                return s.slotComponent.Slot.Equip(new ItemStack(data, 1), inventory: null, ctx: NewCtx());
            }
            return false;
        }

        InventoryContext NewCtx() => new InventoryContext { TargetStats = provider?.Stats };
    }
}
