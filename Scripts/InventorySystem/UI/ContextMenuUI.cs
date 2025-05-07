using UnityEngine;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    /// <summary>Popup that offers Use / Equip / Drop depending on the item.</summary>
    public class ContextMenuUI : MonoBehaviour
    {
        [SerializeField] private Button useBtn;
        [SerializeField] private Button equipBtn;
        [SerializeField] private Button dropBtn;

        private ItemStack stack;
        private Inventory inv;
        private InventoryUI owner;

        public void Open(ItemStack s, Inventory i, RectTransform anchor, InventoryUI o)
        {
            stack = s; inv = i; owner = o;

            bool canUse = stack.Data.Effects.Count > 0;
            bool canEquip = stack.Data.EquipTags.Count > 0;

            useBtn.gameObject.SetActive(canUse);
            equipBtn.gameObject.SetActive(canEquip);

            useBtn.Set(onUse);
            equipBtn.Set(onEquip);
            dropBtn.Set(onDrop);

            ((RectTransform)transform).position =
                anchor.position + new Vector3(GetWidth() * 0.5f, 0);

            gameObject.SetActive(true);
        }

        /* -------- button callbacks -------- */

        void onUse()
        {
            inv.UseItem(stack.Data.Id, new InventoryContext());
            Close();
        }

        void onEquip()
        {
            var bridge = FindObjectOfType<PlayerEquipmentBridge>();
            bridge?.Equip(stack);
            Close();
        }

        void onDrop()
        {
            inv.RemoveItem(stack.Data.Id, stack.Count);
            // TODO: spawn pickup prefab in world
            Close();
        }

        public void Close() => gameObject.SetActive(false);

        float GetWidth() =>
            ((RectTransform)transform).rect.width;
    }

    /* helper to reset & add listeners */
    static class BtnExt
    {
        public static void Set(this Button b, UnityEngine.Events.UnityAction cb)
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(cb);
        }
    }
}
