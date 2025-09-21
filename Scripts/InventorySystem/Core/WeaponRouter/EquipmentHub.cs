using JG.GameContent;
using JG.Inventory.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weapons;

namespace JG.Inventory
{
    /// <summary>
    /// Entry point that bridges the passive inventory, equipment slots, and runtime weapon controller.
    /// Handles Brotato-style weapon auto-pickup and combining while preserving existing UI actions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EquipmentHub : MonoBehaviour
    {
        [Header("Optional explicit links (auto-discovered if null)")]
        [SerializeField] private IInventoryHolder bag;
        public EquipmentUI EquipmentUI;

        [Header("Classification")]
        [Tooltip("At least ONE of these tags marks an item as a weapon (fallback if no EquippWeapon effect is present).")] 
        [SerializeField] private List<string> weaponTags = new() { "Weapon" };

        private IStatsProvider provider;
        private PlayerWeaponController weaponController;
        private Player player;

        private readonly Dictionary<string, ItemDef> weaponItemCache = new(StringComparer.OrdinalIgnoreCase);

        public Inventory Inventory => bag != null ? bag.Get() : null;

        void Awake()
        {
            bag ??= GetComponent<IInventoryHolder>() ?? GetComponentInParent<IInventoryHolder>(true);
            provider = GetComponentInParent<IStatsProvider>();
            weaponController = GetComponent<PlayerWeaponController>() ?? GetComponentInParent<PlayerWeaponController>(true);
            player = GetComponent<Player>() ?? GetComponentInParent<Player>(true);
        }

        public void Init(EquipmentUI equipUI)
        {
            EquipmentUI = equipUI;
            EquipmentUI.Init();
        }

        /* -------------------- Public API -------------------- */

        /// <summary>
        /// Weapons are equipped directly (or combined); other items are routed to the inventory bag.
        /// Returns false if the pickup should be rejected (e.g., no matching slot or combine target).
        /// </summary>
        public bool TryAddItem(IInventoryItem item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            for (int i = 0; i < quantity; i++)
            {
                if (item is ItemDef itemDef && TryResolveWeaponDef(itemDef, out var weaponDef, out _))
                {
                    if (!TryHandleWeaponPickup(itemDef, weaponDef))
                    {
                        Debug.Log($"[{name}] Failed to auto-equip weapon '{itemDef.Id}'.");
                        return false;
                    }
                }
                else if (IsWeapon(item))
                {
                    Debug.LogWarning($"[{name}] Weapon '{item.Id}' lacks an EquippWeapon effect.");
                    return false;
                }
                else
                {
                    if (Inventory == null || !Inventory.AddItem(item, 1))
                    {
                        Debug.Log($"[{name}] Failed to add item '{item.Id}' to inventory bag.");
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Manual UI action: equip from the bag into the first compatible slot.</summary>
        public bool Equip(ItemStack stack)
        {
            if (stack == null || Inventory == null) return false;
            var ctx = NewCtx();

            foreach (var binding in EquipmentUI.Slots)
            {
                if (!binding.slotComponent.Slot.CanEquip(stack.Data)) continue;
                PrepareContext(ctx, binding.slotComponent, stack.Data as ItemDef);
                return binding.slotComponent.Slot.Equip(stack, Inventory, ctx);
            }
            return false;
        }

        /// <summary>Manual UI action: unequip to the bag.</summary>
        public bool Unequip(EquipmentSlotComponent slot) =>
            slot != null && Inventory != null && slot.Slot.Unequip(Inventory, NewCtx());

        /// <summary>Manual UI action: use an item from the bag.</summary>
        public bool Use(ItemStack stack) =>
            stack != null && Inventory != null && Inventory.UseItem(stack.Data.Id, NewCtx());

        /// <summary>Is there an EMPTY slot compatible with this item (weapon-aware)?</summary>
        public bool HasFreeSlotFor(IInventoryItem data)
        {
            if (data is ItemDef item && TryResolveWeaponDef(item, out var weaponDef, out _))
            {
                return EnumerateBindings(weaponDef.slotCategory)
                    .Any(b => b.slotComponent.Slot.Equipped == null);
            }

            if (!IsWeapon(data)) return false;
            return EquipmentUI.Slots.Any(b => b.slotComponent.Slot.Equipped == null && b.slotComponent.Slot.CanEquip(data));
        }

        /// <summary>Sell the weapon equipped in the specified slot for metal.</summary>
        public bool TrySellWeapon(EquipmentSlotComponent slotComponent)
        {
            if (slotComponent == null || slotComponent.Slot == null) return false;
            if (weaponController == null) return false;

            if (!weaponController.TryGetEquippedWeapon(slotComponent.Slot, out var weaponDef, out var itemDef))
                return false;

            int value = ResolveSellValue(weaponDef, itemDef);
            var ctx = NewCtx();
            PrepareContext(ctx, slotComponent, itemDef);
            if (!slotComponent.Slot.Unequip(inventory: null, ctx))
                return false;

            if (player != null)
            {
                ResourceManager.Instance.AddResource(player.PlayerIndex, "metal", value);
            }
            else
            {
                Debug.LogWarning("[EquipmentHub] Sell succeeded but player reference missing.");
            }
            return true;
        }

        /// <summary>Combine this weapon with another of the same tier, equipping the upgraded weapon.</summary>
        public bool TryCombineWeapon(EquipmentSlotComponent primarySlot)
        {
            if (primarySlot == null || primarySlot.Slot == null) return false;
            if (weaponController == null) return false;

            if (!weaponController.TryGetEquippedWeapon(primarySlot.Slot, out var weaponDef, out _))
                return false;

            if (string.IsNullOrWhiteSpace(weaponDef.upgradeResultWeaponId))
                return false;

            if (!ContentCatalogue.Instance.TryGet<WeaponDef>(weaponDef.upgradeResultWeaponId, out _))
                return false;

            if (!TryFindItemForWeapon(weaponDef.upgradeResultWeaponId, out var upgradedItem))
                return false;

            var otherSlotBinding = EnumerateBindings(weaponDef.slotCategory)
                .FirstOrDefault(b => b.slotComponent.Slot != null &&
                                     b.slotComponent.Slot != primarySlot.Slot &&
                                     weaponController.TryGetEquippedWeapon(b.slotComponent.Slot, out var otherDef, out _) &&
                                     otherDef == weaponDef);

            if (otherSlotBinding.slotComponent == null)
                return false;

            var consumeCtx = NewCtx();
            PrepareContext(consumeCtx, otherSlotBinding.slotComponent, otherSlotBinding.slotComponent.Slot.Equipped?.Data as ItemDef);
            otherSlotBinding.slotComponent.Slot.Unequip(inventory: null, consumeCtx);

            var upgradeCtx = NewCtx();
            PrepareContext(upgradeCtx, primarySlot, upgradedItem);
            primarySlot.Slot.Unequip(inventory: null, upgradeCtx);
            primarySlot.Slot.Equip(new ItemStack(upgradedItem, 1), inventory: null, ctx: upgradeCtx);
            return true;
        }

        /// <summary>Unequip without returning the item to the bag (used for drop/sell flows).</summary>
        public bool UnequipToVoid(EquipmentSlotComponent slot) =>
            slot != null && slot.Slot.Unequip(inventory: null, ctx: NewCtx());

        /* -------------------- Internals -------------------- */

        bool TryHandleWeaponPickup(ItemDef item, WeaponDef weaponDef)
        {
            if (weaponController == null)
            {
                Debug.LogWarning("[EquipmentHub] No PlayerWeaponController available; cannot equip weapon.");
                return false;
            }

            // 1) try to use an empty slot of the matching category.
            var emptyBinding = EnumerateBindings(weaponDef.slotCategory)
                .FirstOrDefault(b => b.slotComponent.Slot.Equipped == null);

            if (emptyBinding.slotComponent != null)
            {
                var ctx = NewCtx();
                PrepareContext(ctx, emptyBinding.slotComponent, item);
                Debug.Log($"[{name}] Auto-equipping weapon '{item.Id}' into slot '{emptyBinding.slotComponent.name}'.");
                return emptyBinding.slotComponent.Slot.Equip(new ItemStack(item, 1), inventory: null, ctx: ctx);
            }

            // 2) attempt to combine with a matching weapon already equipped.
            if (TryAutoCombine(weaponDef))
                return true;

            return false;
        }

        bool TryAutoCombine(WeaponDef baseDef)
        {
            if (string.IsNullOrWhiteSpace(baseDef.upgradeResultWeaponId))
                return false;

            if (!ContentCatalogue.Instance.TryGet<WeaponDef>(baseDef.upgradeResultWeaponId, out _))
                return false;

            if (!TryFindItemForWeapon(baseDef.upgradeResultWeaponId, out var upgradedItem))
                return false;

            var existingBinding = EnumerateBindings(baseDef.slotCategory)
                .FirstOrDefault(b => b.slotComponent.Slot.Equipped != null &&
                                     weaponController.TryGetEquippedWeapon(b.slotComponent.Slot, out var currentDef, out _) &&
                                     currentDef == baseDef);

            if (existingBinding.slotComponent == null)
                return false;

            var ctx = NewCtx();
            PrepareContext(ctx, existingBinding.slotComponent, upgradedItem);
            existingBinding.slotComponent.Slot.Unequip(inventory: null, ctx);
            existingBinding.slotComponent.Slot.Equip(new ItemStack(upgradedItem, 1), inventory: null, ctx: ctx);
            Debug.Log($"[{name}] Auto-combined weapon into '{baseDef.upgradeResultWeaponId}'.");
            return true;
        }

        IEnumerable<EquipmentUI.SlotBinding> EnumerateBindings(WeaponSlotCategory category)
        {
            if (EquipmentUI?.Slots == null) yield break;
            foreach (var binding in EquipmentUI.Slots)
            {
                if (binding.slotComponent == null) continue;
                if (binding.slotComponent.SlotCategory == category)
                    yield return binding;
            }
        }

        public bool IsWeapon(IInventoryItem data)
        {
            if (data is ItemDef item && TryResolveWeaponDef(item, out _, out _))
                return true;
            return data != null && data.EquipTags != null && data.EquipTags.Any(weaponTags.Contains);
        }

        bool TryResolveWeaponDef(ItemDef item, out WeaponDef weaponDef, out string weaponId)
        {
            weaponDef = null;
            weaponId = null;
            if (item?.Effects == null) return false;

            foreach (var effect in item.Effects)
            {
                if (!string.Equals(effect.effectType, "EquippWeapon", StringComparison.OrdinalIgnoreCase))
                    continue;

                string id = null;
                var token = effect.effectParams;
                if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                    continue;

                if (token.Type == JTokenType.String)
                {
                    id = token.Value<string>();
                }
                else if (token is JObject obj)
                {
                    id = obj["weaponID"]?.ToString() ?? obj["id"]?.ToString();
                }

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                weaponId = id;
                if (!ContentCatalogue.Instance.TryGet(id, out weaponDef))
                {
                    Debug.LogWarning($"[EquipmentHub] WeaponDef '{id}' referenced by item '{item.Id}' not found.");
                    return false;
                }
                return true;
            }
            return false;
        }

        bool TryFindItemForWeapon(string weaponId, out ItemDef item)
        {
            if (weaponItemCache.TryGetValue(weaponId, out item))
                return item != null;

            item = ContentCatalogue.Instance.GetAll<ItemDef>()
                .FirstOrDefault(def => TryResolveWeaponDef(def, out _, out var resolvedId) &&
                                       string.Equals(resolvedId, weaponId, StringComparison.OrdinalIgnoreCase));
            weaponItemCache[weaponId] = item;
            return item != null;
        }

        int ResolveSellValue(WeaponDef weaponDef, ItemDef itemDef)
        {
            if (weaponDef != null && weaponDef.sellValue > 0)
                return weaponDef.sellValue;
            if (itemDef != null && itemDef.Price > 0)
                return Math.Max(1, itemDef.Price / 2);
            return 1;
        }

        void PrepareContext(IInventoryContext ctx, EquipmentSlotComponent slotComponent, ItemDef item)
        {
            if (ctx is CoDInventoryContext codCtx)
            {
                codCtx.Set(slotComponent != null ? slotComponent.Slot : null);
                codCtx.Set(slotComponent);
                codCtx.Set(item);
            }
        }

        IInventoryContext NewCtx()
        {
            if (Inventory != null && Inventory.ctxFactory != null)
                return Inventory.ctxFactory();

            var stats = provider?.Stats;
            var owner = player ?? GetComponent<Player>() ?? GetComponentInParent<Player>(true);
            return new CoDInventoryContext(stats, owner);
        }
    }
}



