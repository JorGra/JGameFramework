using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace JG.Inventory.UI
{
    /// <summary>Visual representation of one InventorySlot.</summary>
    public class ItemSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text qtyText;

        private ItemStack _stack;
        private InventoryUI _owner;

        public void Init(ItemStack stack, InventoryUI owner)
        {
            _stack = stack;
            _owner = owner;

            icon.sprite = stack.Data.Icon;
            icon.enabled = icon.sprite != null;
            qtyText.text = stack.Count > 1 ? stack.Count.ToString() : "";
        }

        public void OnPointerClick(PointerEventData ev)
        {
            if (ev.button != PointerEventData.InputButton.Right) return;
            _owner.ShowContextMenu(_stack, transform as RectTransform);
        }
    }
}
