using UnityEngine;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    /// <summary>Right-click / pad-button menu for inventory & equipment slots.</summary>
    public class ContextMenuUI : MonoBehaviour
    {
        [SerializeField] private Button useBtn, equipBtn, unequipBtn, dropBtn;

        ItemStack stack;
        Inventory inv;
        EquipmentSlotComponent equipSlot;
        PlayerEquipmentBridge bridge;

        public void Open(ItemStack s,
                         Inventory i,
                         RectTransform anchor,
                         PlayerEquipmentBridge br,
                         EquipmentSlotComponent slotComponent = null)
        {
            stack = s;
            inv = i;
            equipSlot = slotComponent;
            bridge = br ?? slotComponent?.GetComponentInParent<PlayerEquipmentBridge>();

            /* ---------- button visibility ---------- */
            bool canEquip = !IsEquipped && stack.Data.EquipTags.Count > 0;
            bool canUse = stack.Data.Effects.Count > 0 && stack.Data.EquipTags.Count == 0;
            bool canUnequip = IsEquipped;

            equipBtn.gameObject.SetActive(canEquip);
            useBtn.gameObject.SetActive(canUse);
            unequipBtn.gameObject.SetActive(canUnequip);

            /* ---------- callbacks ---------- */
            useBtn.Set(OnUse);
            equipBtn.Set(OnEquip);
            unequipBtn.Set(OnUnequip);
            dropBtn.Set(OnDrop);

            ((RectTransform)transform).position =
                anchor.position + new Vector3(GetWidth() * .5f, 0);

            gameObject.SetActive(true);
        }

        /* ----- operations ----- */

        void OnUse() { bridge?.Use(stack); Close(); }
        void OnEquip() { bridge?.Equip(stack); Close(); }
        void OnUnequip() { 
            if(bridge == null)
            {
                Debug.LogError("Bridge null on Unequip");
                return;
            }
        
            bridge.Unequip(equipSlot); 
            Close(); 
        }
        void OnDrop()
        {
            inv.RemoveItem(stack.Data.Id, stack.Count);
            Close();
        }

        /* ----- helpers ----- */

        public void Close() => gameObject.SetActive(false);
        float GetWidth() => ((RectTransform)transform).rect.width;
        bool IsEquipped => equipSlot != null;
    }
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
