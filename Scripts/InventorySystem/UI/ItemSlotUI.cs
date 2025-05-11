using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    /// <summary>Visual representation of one inventory stack.</summary>
    public class ItemSlotUI : MonoBehaviour,
                              IPointerClickHandler,
                              IPointerEnterHandler, IPointerExitHandler,
                              ISelectHandler, IDeselectHandler
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text qtyText;

        ItemStack stack;
        IContextMenuHost owner;          // ← was InventoryUI
        ItemDetailPanelUI detailPanel;

        public void Init(ItemStack s, IContextMenuHost o, ItemDetailPanelUI panel)
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
            qtyText.text = stack.Count > 1 ? stack.Count.ToString() : "";
        }

        /* ───────── input ───────── */

        public void OnPointerClick(PointerEventData ev)
        {
            if (ev.button != PointerEventData.InputButton.Right) return;
            owner?.ShowContextMenu(stack, transform as RectTransform);
        }

        /* ───────── tooltip passthrough ───────── */

        public void OnPointerEnter(PointerEventData _) => detailPanel?.Show(stack);
        public void OnPointerExit(PointerEventData _) => detailPanel?.Hide();
        public void OnSelect(BaseEventData _) => detailPanel?.Show(stack);
        public void OnDeselect(BaseEventData _) => detailPanel?.Hide();
    }
}
