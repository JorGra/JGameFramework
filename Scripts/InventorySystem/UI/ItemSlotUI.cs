using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JGameFramework.UI.Tooltips;

namespace JG.Inventory.UI
{
    /// <summary>Visual representation of one inventory stack.</summary>
    public class ItemSlotUI : MonoBehaviour,
                              IPointerClickHandler,
                              IPointerEnterHandler, IPointerExitHandler,
                              ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text qtyText;
        [SerializeField] private Vector2 tooltipOffset = new Vector2(18f, 18f);

        private ItemStack stack;
        private IContextMenuHost owner;
        private ItemDetailPanelUI detailPanel;
        private PlayerTooltipController tooltipController;

        public RectTransform RectTransform => transform as RectTransform;

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
            if (stack == null || stack.Data == null)
            {
                if (icon != null)
                {
                    icon.enabled = false;
                    icon.sprite = null;
                }

                if (qtyText != null)
                {
                    qtyText.text = string.Empty;
                }

                return;
            }

            if (icon != null)
            {
                icon.sprite = stack.Data.Icon;
                icon.enabled = icon.sprite != null;
            }

            if (qtyText != null)
            {
                qtyText.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
            }
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
            RefreshVisuals();
            HideTooltip();
        }

        private void ShowTooltip(BaseEventData eventData)
        {
            if (tooltipController == null || stack == null || stack.Data == null)
            {
                return;
            }

            tooltipController.ShowTooltip(
                owner: this,
                anchor: RectTransform,
                followTarget: true,
                configure: builder =>
                {
                    builder
                        .WithOffset(tooltipOffset)
                        .WithClampOverride(true)
                        .AddContent(new TooltipTextBlockData
                        {
                            Header = stack.Data.DisplayName,
                            //Body = stack.Data.,
                            ShowHeader = true
                        });
                },
                eventData: eventData);
        }

        private void HideTooltip()
        {
            tooltipController?.CloseTooltip(this);
        }
    }
}
