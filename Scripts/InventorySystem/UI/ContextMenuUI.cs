using UnityEngine;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Popup with Use / Equip / Drop / Unequip depending on context.
    /// </summary>
    public class ContextMenuUI : MonoBehaviour
    {
        [SerializeField] private Button useBtn;
        [SerializeField] private Button equipBtn;
        [SerializeField] private Button unequipBtn;
        [SerializeField] private Button dropBtn;

        ItemStack stack;
        Inventory inv;
        EquipmentSlotComponent equipSlot;          // ≠ null → we’re on an equipped item

        /// <summary>
        /// Opens the popup.
        /// </summary>
        public void Open(ItemStack s,
                         Inventory i,
                         RectTransform anchor,
                         InventoryUI owner = null,
                         EquipmentSlotComponent slotComponent = null)
        {
            stack = s;
            inv = i;
            equipSlot = slotComponent;

            bool canUse = stack.Data.Effects.Count > 0;
            bool canEquip = !IsEquipped && stack.Data.EquipTags.Count > 0;
            bool canUnequip = IsEquipped;

            useBtn.gameObject.SetActive(canUse);
            equipBtn.gameObject.SetActive(canEquip);
            unequipBtn.gameObject.SetActive(canUnequip);

            useBtn.Set(OnUse);
            equipBtn.Set(OnEquip);
            unequipBtn.Set(OnUnequip);
            dropBtn.Set(OnDrop);

            /* place right of the slot */
            ((RectTransform)transform).position =
                anchor.position + new Vector3(GetWidth() * .5f, 0);

            gameObject.SetActive(true);
        }

        /* ───────── button callbacks ───────── */

        void OnUse()
        {
            inv.UseItem(stack.Data.Id, new InventoryContext());
            Close();
        }

        void OnEquip()
        {
            FindObjectOfType<PlayerEquipmentBridge>()?.Equip(stack);
            Close();
        }

        void OnUnequip()
        {
            if (!IsEquipped) return;

            /* move back to inventory, then clear slot */
            if (inv.AddItem(stack.Data, stack.Count))
            {
                equipSlot.Slot.Unequip(new InventoryContext());                       // extension method below
            }
            Close();
        }

        void OnDrop()
        {
            inv.RemoveItem(stack.Data.Id, stack.Count);
            // TODO: spawn world pickup
            Close();
        }

        /* ───────── helpers ───────── */

        public void Close() => gameObject.SetActive(false);
        float GetWidth() => ((RectTransform)transform).rect.width;
        bool IsEquipped => equipSlot != null;
    }

    /* thin helpers ----------------------------------------------------------- */
    static class BtnExt
    {
        public static void Set(this Button b, UnityEngine.Events.UnityAction cb)
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(cb);
        }
    }
}
