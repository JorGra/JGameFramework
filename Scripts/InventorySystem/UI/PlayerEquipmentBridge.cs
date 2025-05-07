using System.Collections.Generic;
using UnityEngine;
using JG.Inventory;           // for InventoryContext & ItemEffect*

namespace JG.Inventory.UI
{
    /// <summary>
    /// Mediates between an <see cref="Inventory"/> and the player’s equipment +
    /// Stats container.  All item effects marked for “equip” are applied while
    /// the item is worn and removed automatically on unequip.
    /// </summary>
    public class PlayerEquipmentBridge : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private Transform slotContainer;
        private EquipmentSlotComponent[] slots;
        private Stats targetStats;
        public Stats TargetStats                                    // NEW – runtime assignment
        {
            get => targetStats;
            set
            {
                targetStats = value;
                if (ctx != null) ctx.TargetStats = value;          // keep context in sync
            }
        }
        readonly Dictionary<EquipmentSlotComponent, List<IItemEffect>> active =
            new();

        InventoryContext ctx;                                           // reused object
        bool awakeDone;

        void Awake()
        {
            if (slots.Length == 0 && slotContainer != null)
                slots = slotContainer.GetComponentsInChildren<EquipmentSlotComponent>(true);

            if (inventory == null)
                inventory = GetComponentInParent<InventoryComponent>();

            ctx = new InventoryContext { TargetStats = this.targetStats };

            awakeDone = true;
        }

        /* ───────────── public API ───────────── */

        public bool Equip(ItemStack stack)
        {
            EnsureAwake();

            foreach (var s in slots)
            {
                if (!s.Slot.CanEquip(stack.Data)) continue;

                /* remove ONE copy from inventory first */
                if (!inventory.Runtime.RemoveItem(stack.Data.Id, 1)) return false;

                var equipStack = new ItemStack(stack.Data, 1);
                if (!s.Slot.Equip(equipStack, ctx))                     // pass context ↓
                {                                                       // slot refused
                    inventory.Runtime.AddItem(stack.Data, 1);
                    return false;
                }

                ApplyEquipEffects(s, equipStack);
                return true;
            }
            return false;
        }

        public bool Unequip(EquipmentSlotComponent slot)
        {
            EnsureAwake();

            var stack = slot.Slot.Equipped;
            if (stack == null){
                Debug.Log("slot null");
                return false; 
            }
            /* 1) Remove *all* modifiers that came from this slot */
            if (active.TryGetValue(slot, out var list))
            {
                foreach (var e in list)
                    e.Remove(ctx);
                active.Remove(slot);
            }
            else
            {
                /* fallback: rebuild effects and remove them */
                foreach (var def in stack.Data.Effects)
                    ItemEffectRegistry.Build(def.effectType, def.effectParams)
                                       ?.Remove(ctx);
            }

            /* 2) Give the item back to the inventory */
            if (!inventory.Runtime.AddItem(stack.Data, stack.Count))
                return false;                                      // inventory full

            /* 3) Visually / logically empty the slot */
            slot.Slot.Unequip(ctx);
            return true;
        }

        /// <summary>Consumes one unit, applying “use” effects.</summary>
        public bool Use(ItemStack stack) =>
            inventory.Runtime.UseItem(stack.Data.Id, ctx);

        /* ─────────── helpers ─────────── */

        void ApplyEquipEffects(EquipmentSlotComponent slot, ItemStack stack)
        {
            var list = new List<IItemEffect>();
            foreach (var def in stack.Data.Effects)
            {
                var e = ItemEffectRegistry.Build(def.effectType, def.effectParams);
                if (e == null) continue;

                e.Apply(ctx);
                list.Add(e);
            }
            active[slot] = list;
        }

        void EnsureAwake() { if (!awakeDone) Awake(); }
    }
}
