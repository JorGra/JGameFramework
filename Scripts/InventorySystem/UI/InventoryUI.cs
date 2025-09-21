using System.Collections.Generic;
using UnityEngine;
using JGameFramework.UI.Tooltips;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Scroll-view-based inventory list for a single player.
    /// Keeps item Slots and tooltip panel in sync with the runtime inventory.
    /// </summary>
    public class InventoryUI : MonoBehaviour, IContextMenuHost
    {
        [Header("References")]
        [SerializeField] private InventoryComponent playerInventory;
        [SerializeField] private EquipmentHub equipmentHub;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private ItemSlotUI slotPrefab;
        [Tooltip("Tooltip panel that belongs to THIS inventory window.")]
        [SerializeField] private ItemDetailPanelUI detailPanel;

        [Header("Context Menu")]
        [SerializeField] private PlayerTooltipController tooltipController;
        [SerializeField] private Vector2 contextMenuOffset = new Vector2(0f, 18f);

        private readonly List<ItemSlotUI> _pool = new();

        public void SetTooltipController(PlayerTooltipController controller) => tooltipController = controller;

        private void Awake()
        {
            if (playerInventory == null)
                playerInventory = GetComponentInParent<InventoryComponent>();

            if (playerInventory == null)
            {
                Debug.LogError($"{name}: No InventoryComponent assigned or found.");
                enabled = false;
                return;
            }

            if (equipmentHub == null)
                equipmentHub = GetComponentInParent<EquipmentHub>();

            playerInventory.Get().Changed += Rebuild;
        }

        private void OnEnable() => Rebuild();

        private void OnDisable()
        {
            tooltipController?.CloseAllContextMenus();
        }

        /// <summary>Called by <see cref="ItemSlotUI"/> to open the context menu.</summary>
        public void ShowContextMenu(ItemStack stack, RectTransform anchor)
        {
            if (stack == null || anchor == null || tooltipController == null)
            {
                return;
            }

            var inventory = playerInventory.Get();
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

                    bool hasEffects = stack.Data.Effects != null && stack.Data.Effects.Count > 0;
                    bool hasEquipTags = stack.Data.EquipTags != null && stack.Data.EquipTags.Count > 0;
                    bool canEquip = equipmentHub != null && hasEquipTags;
                    bool canUse = hasEffects;

                    if (canUse)
                    {
                        builder.AddAction(new TooltipActionData(
                            label: "Use",
                            callback: (handle, ctx) =>
                            {
                                bool used = false;
                                if (equipmentHub != null)
                                {
                                    used = equipmentHub.Use(stack);
                                }

                                if (!used)
                                {
                                    var invInstance = playerInventory.Get();
                                    if (invInstance != null)
                                    {
                                        invInstance.UseItem(stack.Data.Id, invInstance.ctxFactory());
                                    }
                                }
                            }));
                    }

                    if (canEquip)
                    {
                        builder.AddAction(new TooltipActionData(
                            label: "Equip",
                            callback: (handle, ctx) =>
                            {
                                equipmentHub?.Equip(stack);
                            },
                            interactable: equipmentHub != null));
                    }

                    builder.AddAction(new TooltipActionData(
                        label: "Drop",
                        callback: (handle, ctx) =>
                        {
                            var invInstance = playerInventory.Get();
                            invInstance?.RemoveItem(stack.Data.Id, stack.Count);
                        }));
                },
                eventData: null,
                contextOverride: null,
                followTarget: false);
        }

        private void Rebuild()
        {
            var inventory = playerInventory.Get();
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
                    tooltipController?.CloseTooltip(slot);
                    continue;
                }

                slot.gameObject.SetActive(true);
                slot.ConfigureTooltip(tooltipController);
                slot.Init(inventory.Slots[i].Stack, this, detailPanel);
            }
        }
    }
}
