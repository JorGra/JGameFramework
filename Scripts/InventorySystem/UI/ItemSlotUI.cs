using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Visual representation of one inventory stack (icon + quantity).
    /// </summary>
    public class ItemSlotUI : MonoBehaviour,
                              IPointerClickHandler,
                              IPointerEnterHandler, IPointerExitHandler,
                              ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text qtyText;

        ItemStack stack;
        InventoryUI owner;
        ItemDetailPanelUI detailPanel;

        /// <summary>Initialises the slot.</summary>
        public void Init(ItemStack s, InventoryUI o, ItemDetailPanelUI panel)
        {
            stack = s;
            owner = o;
            detailPanel = panel;

            RefreshVisuals();
        }

        /* ───────── visuals ───────── */

        void RefreshVisuals()
        {
            icon.sprite = stack.Data.Icon;
            icon.enabled = icon.sprite != null;
            qtyText.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
        }

        /* ───────── input ───────── */

        public void OnPointerClick(PointerEventData ev)
        {
            if (ev.button != PointerEventData.InputButton.Right) return;
            owner.ShowContextMenu(stack, transform as RectTransform);
        }

        /* ───────── hover / focus ⇒ tooltip ───────── */

        public void OnPointerEnter(PointerEventData _) => detailPanel?.Show(stack);
        public void OnPointerExit(PointerEventData _) => detailPanel?.Hide();
        public void OnSelect(BaseEventData _) => detailPanel?.Show(stack);
        public void OnDeselect(BaseEventData _) => detailPanel?.Hide();
    }
}
