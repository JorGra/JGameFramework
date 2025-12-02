using UnityEngine;
using UnityEngine.EventSystems;
using JGameFramework.UI.Tooltips;
using JG.GameContent;

namespace JG.Inventory.UI
{
    /// <summary>Visual representation of one inventory stack.</summary>
    public class ItemSlotUI : MonoBehaviour,
                              IPointerClickHandler,
                              IPointerEnterHandler, IPointerExitHandler,
                              ISelectHandler, IDeselectHandler
    {
        [SerializeField] private ItemViewWidget itemView;
        [SerializeField] private Vector2 tooltipOffset = new Vector2(18f, 18f);

        private ItemStack stack;
        private IContextMenuHost owner;
        private ItemDetailPanelUI detailPanel;
        private PlayerTooltipController tooltipController;

        public RectTransform RectTransform => transform as RectTransform;

        private void Awake()
        {
            if (!itemView)
            {
                itemView = GetComponent<ItemViewWidget>();
            }
        }

        private void OnValidate()
        {
            if (!itemView)
            {
                itemView = GetComponent<ItemViewWidget>();
            }
        }

        public void Init(ItemStack s, IContextMenuHost o, ItemDetailPanelUI panel)
        {
            stack = s;
            owner = o;
            detailPanel = panel;
            RefreshVisuals();
        }

        public void ConfigureTooltip(PlayerTooltipController controller)
        {
            tooltipController = controller;
        }

        private void RefreshVisuals()
        {
            if (itemView == null)
            {
                return;
            }

            if (stack == null || stack.Data == null)
            {
                itemView.Clear();
                return;
            }

            if (!TryResolveItemDef(out var itemDef))
            {
                itemView.Clear();
                return;
            }

            var data = ItemViewData.FromItemDef(itemDef, stackCount: stack.Count, hideStackIfOne: true);
            itemView.Apply(data);
        }

        public void OnPointerClick(PointerEventData ev)
        {
            if (ev.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            owner?.ShowContextMenu(stack, RectTransform);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            detailPanel?.Show(stack);
            ShowTooltip(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            detailPanel?.Hide();
            HideTooltip();
        }

        public void OnSelect(BaseEventData eventData)
        {
            detailPanel?.Show(stack);
            ShowTooltip(eventData);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            detailPanel?.Hide();
            HideTooltip();
        }

        private void OnDisable()
        {
            detailPanel?.Hide();
            HideTooltip();
        }

        public void ClearSlot()
        {
            stack = null;
            owner = null;
            detailPanel = null;
            itemView?.Clear();
            HideTooltip();
        }

        private void ShowTooltip(BaseEventData eventData)
        {
            if (tooltipController == null || stack == null || stack.Data == null)
            {
                return;
            }

            if (!TryResolveItemDef(out var itemDef))
            {
                return;
            }

            var context = new ItemTooltipContext(
                owner: this,
                item: itemDef,
                anchor: RectTransform,
                eventData: eventData,
                fallbackOffset: tooltipOffset);

            tooltipController.ShowItemTooltip(context);
        }

        private bool TryResolveItemDef(out ItemDef itemDef)
        {
            itemDef = null;

            if (stack?.Data is ItemDef directDef)
            {
                itemDef = directDef;
                return true;
            }

            if (stack?.Data == null)
            {
                return false;
            }

            if (ContentCatalogue.Instance.TryGet(stack.Data.Id, out itemDef))
            {
                return true;
            }

            return false;
        }

        private void HideTooltip()
        {
            tooltipController?.CloseTooltip(this);
        }
    }
}
