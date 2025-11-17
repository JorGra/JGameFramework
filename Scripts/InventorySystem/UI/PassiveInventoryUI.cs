using System.Collections.Generic;
using UnityEngine;
using JGameFramework.UI.Tooltips;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Scroll-view list for the "passive-bonus" inventory. Keeps item Slots,
    /// tooltip and context menu in sync with <see cref="PassiveInventoryComponent"/>.
    /// </summary>
    public class PassiveInventoryUI : MonoBehaviour, IContextMenuHost
    {
        [Header("References")]
        [SerializeField] private IInventoryHolder passiveInventory;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private ItemSlotUI slotPrefab;
        [Tooltip("Tooltip panel that belongs to THIS inventory window.")]
        [SerializeField] private ItemDetailPanelUI detailPanel;

        [Header("Context Menu")]
        [SerializeField] private PlayerTooltipController tooltipController;
        [SerializeField] private Vector2 contextMenuOffset = new Vector2(0f, 18f);

        [SerializeField] private bool autoSetup = true;

        private readonly List<ItemSlotUI> _pool = new();

        public void SetTooltipController(PlayerTooltipController controller) => tooltipController = controller;

        private void Awake()
        {
            if (autoSetup)
            {
                Setup();
            }
        }
        public void Setup()
        {
            if (passiveInventory == null)
            {
                passiveInventory = GetComponentInParent<PassiveInventoryComponent>();
            }

            if (passiveInventory == null)
            {
                Debug.LogError($"{name}: No PassiveInventoryComponent assigned or found.");
                enabled = false;
                return;
            }

            passiveInventory.Get().Changed += Rebuild;
        }

        private void OnEnable() => Rebuild();

        private void OnDisable()
        {
            tooltipController?.CloseAllContextMenus();
        }

        public void ShowContextMenu(ItemStack stack, RectTransform anchor)
        {
            if (stack == null || anchor == null || tooltipController == null)
            {
                return;
            }

            var inventory = passiveInventory?.Get();
            if (inventory == null)
            {
                return;
            }

            tooltipController.ShowContextMenu(
                owner: anchor,
                anchor: anchor,
                configure: builder =>
                {
                    builder
                        .WithOffset(contextMenuOffset)
                        .WithPivot(new Vector2(0.5f, 0f))
                        .WithClampOverride(true);

                    builder.AddContent(new TooltipTextBlockData
                    {
                        Header = stack.Data.DisplayName,
                        Body = string.Empty,
                        ShowHeader = true
                    });

                    builder.AddContent(new TooltipKeyValueRowData
                    {
                        Label = "Quantity",
                        Value = stack.Count.ToString()
                    });

                    bool hasEffects = ((stack.Data.Effects?.Count ?? 0) > 0);
                    if (hasEffects)
                    {
                        builder.AddAction(new TooltipActionData(
                            label: "Use",
                            callback: (handle, ctx) =>
                            {
                                inventory.UseItem(stack.Data.Id, inventory.ctxFactory());
                            }));
                    }

                    builder.AddAction(new TooltipActionData(
                        label: "Drop",
                        callback: (handle, ctx) =>
                        {
                            inventory.RemoveItem(stack.Data.Id, stack.Count);
                        }));
                },
                eventData: null,
                contextOverride: null,
                followTarget: false);
        }

        private void Rebuild()
        {
            var inventory = passiveInventory?.Get();
            if (inventory == null) return;

            int needed = inventory.Slots.Count;

            while (_pool.Count < needed)
            {
                _pool.Add(Instantiate(slotPrefab, contentRoot));
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < needed;
                var slot = _pool[i];

                if (!active)
                {
                    tooltipController?.CloseContextMenu(slot.RectTransform);
                    slot.gameObject.SetActive(false);
                    continue;
                }

                slot.gameObject.SetActive(true);
                slot.Init(inventory.Slots[i].Stack, this, detailPanel);
            }
        }
    }
}
