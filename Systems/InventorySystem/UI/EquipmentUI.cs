using JG.GameContent;
using JGameFramework.UI.Tooltips;
using System;
using System.Collections.Generic;
using UI.Theming;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    /// <summary>Displays and refreshes all equipment Slots.</summary>
    public class EquipmentUI : MonoBehaviour
    {
        [System.Serializable]
        public struct SlotBinding
        {
            public Button button;
            public EquipmentSlotComponent slotComponent;
            public ItemViewWidget itemView;
        }

        [Header("Auto-populated")]
        public List<SlotBinding> Slots = new();

        [Header("Scan these parents for slots (leave empty to use this object)")]
        [SerializeField] private List<Transform> slotContainers = new();

        [Header("Context Menu")]
        [SerializeField] private PlayerTooltipController tooltipController;
        [SerializeField] private Vector2 contextMenuOffset = new Vector2(0f, 18f);

        [Header("Item Tooltip")]
        [SerializeField] private bool showSlotTooltips = true;
        [SerializeField] private Vector2 tooltipOffset = new Vector2(24f, 16f);
        [SerializeField] private RarityColorSettings rarityColorSettings;

        public EquipmentHub hub;

        private readonly Dictionary<EquipmentSlotComponent, Action> slotChangedHandlers = new();
        private readonly Dictionary<EquipmentSlotComponent, SlotTooltipBinding> tooltipBindings = new();

        private struct SlotTooltipBinding
        {
            public EventTrigger trigger;
            public EventTrigger.Entry pointerEnter;
            public EventTrigger.Entry pointerExit;
            public EventTrigger.Entry select;
            public EventTrigger.Entry deselect;
        }

        [ContextMenu("Auto-bind + Init")]
        private void AutoBindAndInit()
        {
            RebuildBindings();
            Init();
        }

        [ContextMenu("Auto-bind (no Init)")]
        private void AutoBindOnly() => RebuildBindings();

        public void SetTooltipController(PlayerTooltipController controller) => tooltipController = controller;

        public void Init()
        {
            hub ??= GetComponentInParent<EquipmentHub>(true);

            ResetSlotListeners();

            if (Slots.Count == 0)
            {
                RebuildBindings();
            }

            foreach (var raw in Slots)
            {
                var binding = raw;

                if (!ValidateBinding(binding))
                {
                    continue;
                }

                if (binding.button != null)
                {
                    binding.button.onClick.RemoveAllListeners();
                    binding.button.onClick.AddListener(() => OnSlotClicked(binding));
                }

                BindSlot(binding);
            }
        }

        private void OnDisable()
        {
            if (tooltipController == null)
            {
                return;
            }

            foreach (var raw in Slots)
            {
                if (raw.slotComponent != null)
                {
                    tooltipController.CloseContextMenu(raw.slotComponent);
                    HideSlotTooltip(raw.slotComponent);
                }
            }
        }
        private void OnEnable()
        {
            foreach (var raw in Slots)
            {
                var binding = raw;
                if (binding.slotComponent?.Slot != null)
                {
                    Refresh(binding);
                }
            }
        }

        private void OnSlotClicked(SlotBinding binding)
        {
            if (tooltipController == null || binding.button == null || binding.slotComponent == null)
            {
                return;
            }

            var slot = binding.slotComponent.Slot;
            var equipped = slot?.Equipped;
            if (equipped == null)
            {
                return;
            }

            bool isWeapon = hub != null && hub.IsWeapon(equipped.Data);
            var inventory = hub != null ? hub.Inventory : null;
            var anchor = binding.button.transform as RectTransform;

            HideSlotTooltip(binding.slotComponent);

            tooltipController.ShowContextMenu(
                owner: binding.slotComponent,
                anchor: anchor,
                configure: builder =>
                {
                    builder
                        .WithOffset(contextMenuOffset)
                        .WithPivot(new Vector2(0.5f, 0f))
                        .WithClampOverride(false);

                    builder.AddContent(new TooltipTextBlockData
                    {
                        Header = equipped.Data.DisplayName,
                        Body = string.Empty,
                        ShowHeader = true
                    });

                    builder.AddContent(new TooltipKeyValueRowData
                    {
                        Label = "Slot",
                        Value = binding.slotComponent.name
                    });

                    if (isWeapon)
                    {
                        builder.AddAction(new TooltipActionData(
                            label: "Combine",
                            icon: ThemeManager.Instance.CurrentTheme.GetSprite("IconUpgrade"),
                            callback: (handle, ctx) =>
                            {
                                if (!(hub?.TryCombineWeapon(binding.slotComponent) ?? false))
                                {
                                    Debug.Log("[EquipmentUI] Combine failed: no matching weapon available.");
                                }
                                handle.Close();
                            }));

                        builder.AddAction(new TooltipActionData(
                            label: "Sell",
                            icon: ThemeManager.Instance.CurrentTheme.GetSprite("IconSell"),
                            callback: (handle, ctx) =>
                            {
                                if (!(hub?.TrySellWeapon(binding.slotComponent) ?? false))
                                {
                                    Debug.Log("[EquipmentUI] Sell failed.");
                                }
                                handle.Close();
                            }));
                    }
                    else
                    {
                        bool canUse = ((equipped.Data.Effects?.Count ?? 0) > 0) && equipped.Data.EquipTags.Count == 0;

                        {
                            builder.AddAction(new TooltipActionData(
                                label: "Use",
                                callback: (handle, ctx) => hub?.Use(equipped)));
                        }

                        if (hub != null)
                        {
                            builder.AddAction(new TooltipActionData(
                                label: "Unequip",
                                callback: (handle, ctx) => hub.Unequip(binding.slotComponent)));

                            builder.AddAction(new TooltipActionData(
                                label: "Drop",
                                callback: (handle, ctx) =>
                                {
                                    if (inventory != null)
                                    {
                                        if (hub.Unequip(binding.slotComponent))
                                        {
                                            inventory.RemoveItem(equipped.Data.Id, equipped.Count);
                                        }
                                    }
                                    else
                                    {
                                        hub.UnequipToVoid(binding.slotComponent);
                                    }
                                }));
                        }
                    }

                    builder.AddAction(new TooltipActionData(
                        label: "Cancel",
                        icon: ThemeManager.Instance.CurrentTheme.GetSprite("IconClose"),
                        callback: (handle, ctx) => handle.Close()));
                },
                eventData: null,
                contextOverride: null,
                followTarget: false);
        }

        private void HideSlotTooltip(EquipmentSlotComponent slotComponent)
        {
            if (slotComponent == null || tooltipController == null)
            {
                return;
            }

            tooltipController.CloseTooltip(slotComponent);
        }

        private void ShowSlotTooltip(SlotBinding binding, BaseEventData eventData)
        {
            if (!showSlotTooltips || tooltipController == null)
            {
                return;
            }

            var slotComponent = binding.slotComponent;
            if (slotComponent == null)
            {
                return;
            }

            if (!TryGetEquippedItem(slotComponent, out var itemDef) || itemDef == null)
            {
                return;
            }

            var anchor = ResolveTooltipAnchor(binding);
            var context = new ItemTooltipContext(
                owner: slotComponent,
                item: itemDef,
                anchor: anchor,
                eventData: eventData,
                fallbackOffset: tooltipOffset);

            tooltipController.ShowItemTooltip(context);
        }

        private RectTransform ResolveTooltipAnchor(SlotBinding binding)
        {
            if (binding.itemView != null)
            {
                return binding.itemView.transform as RectTransform;
            }

            if (binding.button != null)
            {
                return binding.button.transform as RectTransform;
            }

            return binding.slotComponent != null ? binding.slotComponent.transform as RectTransform : null;
        }

        private void ConfigureTooltip(SlotBinding binding)
        {
            var slotComponent = binding.slotComponent;
            RemoveTooltipBinding(slotComponent);

            if (!showSlotTooltips || tooltipController == null || slotComponent == null)
            {
                return;
            }

            var targetGO = binding.button != null
                ? binding.button.gameObject
                : binding.itemView != null
                    ? binding.itemView.gameObject
                    : slotComponent.gameObject;

            if (targetGO == null)
            {
                return;
            }

            var trigger = targetGO.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = targetGO.AddComponent<EventTrigger>();
            }

            var entries = trigger.triggers;
            if (entries == null)
            {
                entries = new List<EventTrigger.Entry>();
                trigger.triggers = entries;
            }

            var localBinding = binding;

            EventTrigger.Entry AddEntry(EventTriggerType type, UnityAction<BaseEventData> callback)
            {
                var entry = new EventTrigger.Entry
                {
                    eventID = type,
                    callback = new EventTrigger.TriggerEvent()
                };
                entry.callback.AddListener(callback);
                entries.Add(entry);
                return entry;
            }

            var pointerEnter = AddEntry(EventTriggerType.PointerEnter, data => ShowSlotTooltip(localBinding, data));
            var pointerExit = AddEntry(EventTriggerType.PointerExit, data => HideSlotTooltip(localBinding.slotComponent));
            var select = AddEntry(EventTriggerType.Select, data => ShowSlotTooltip(localBinding, data));
            var deselect = AddEntry(EventTriggerType.Deselect, data => HideSlotTooltip(localBinding.slotComponent));

            tooltipBindings[slotComponent] = new SlotTooltipBinding
            {
                trigger = trigger,
                pointerEnter = pointerEnter,
                pointerExit = pointerExit,
                select = select,
                deselect = deselect
            };
        }

        private void RemoveTooltipBinding(EquipmentSlotComponent component)
        {
            if (component == null)
            {
                return;
            }

            if (!tooltipBindings.TryGetValue(component, out var binding))
            {
                return;
            }

            var trigger = binding.trigger;
            if (trigger != null && trigger.triggers != null)
            {
                trigger.triggers.Remove(binding.pointerEnter);
                trigger.triggers.Remove(binding.pointerExit);
                trigger.triggers.Remove(binding.select);
                trigger.triggers.Remove(binding.deselect);
            }

            tooltipBindings.Remove(component);
        }

        private bool TryGetEquippedItem(EquipmentSlotComponent component, out ItemDef itemDef)
        {
            itemDef = null;

            var slot = component?.Slot;
            var stack = slot?.Equipped;
            var data = stack?.Data;
            if (data == null)
            {
                return false;
            }

            if (data is ItemDef directDef)
            {
                itemDef = directDef;
                return true;
            }

            if (!string.IsNullOrEmpty(data.Id) && ContentCatalogue.Instance.TryGet<ItemDef>(data.Id, out var resolved))
            {
                itemDef = resolved;
                return true;
            }

            return false;
        }

        private bool ValidateBinding(SlotBinding binding)
        {
            if (binding.slotComponent == null)
            {
                Debug.LogWarning($"{name}: Slot binding missing EquipmentSlotComponent.", this);
                return false;
            }

            return true;
        }

        private void BindSlot(SlotBinding binding)
        {
            if (binding.slotComponent == null)
            {
                return;
            }

            binding.slotComponent.EnsureSlot();
            var slotRef = binding.slotComponent.Slot;
            if (slotRef == null)
            {
                Debug.LogWarning($"{name}: Slot '{binding.slotComponent.name}' did not provide an EquipmentSlot instance.", this);
                return;
            }

            if (slotChangedHandlers.TryGetValue(binding.slotComponent, out var existingHandler))
            {
                slotRef.Changed -= existingHandler;
            }

            Action handler = () => Refresh(binding);
            slotChangedHandlers[binding.slotComponent] = handler;
            slotRef.Changed += handler;

            ConfigureTooltip(binding);
            Refresh(binding);
        }

        private void ResetSlotListeners()
        {
            if (slotChangedHandlers.Count == 0)
            {
                return;
            }

            foreach (var pair in slotChangedHandlers)
            {
                var component = pair.Key;
                if (component == null)
                {
                    continue;
                }

                try
                {
                    var slot = component.Slot;
                    if (slot != null)
                    {
                        slot.Changed -= pair.Value;
                    }

                    RemoveTooltipBinding(component);
                    HideSlotTooltip(component);
                }
                catch (MissingReferenceException)
                {
                    // Slot or component already destroyed; nothing to clean up.
                }
            }

            slotChangedHandlers.Clear();
            tooltipBindings.Clear();
        }


        private void Refresh(SlotBinding binding)
        {
            var slotInstance = binding.slotComponent?.Slot;
            if (slotInstance == null)
            {
                return;
            }

            var equipped = slotInstance.Equipped;
            bool hasItem = equipped != null && equipped.Data != null;

            if (!hasItem)
            {
                tooltipController?.CloseContextMenu(binding.slotComponent);
                HideSlotTooltip(binding.slotComponent);
                binding.itemView?.Clear();
                return;
            }

            if (binding.itemView == null)
            {
                return;
            }

            if (!TryGetEquippedItem(binding.slotComponent, out var itemDef) || itemDef == null)
            {
                binding.itemView.Clear();
                return;
            }

            var data = ItemViewData.FromItemDef(itemDef, stackCount: equipped.Count, hideStackIfOne: true);
            binding.itemView.Apply(data);
        }

        public void RebuildBindings()
        {
            ResetSlotListeners();
            Slots.Clear();

            var roots = slotContainers != null && slotContainers.Count > 0
                ? slotContainers
                : new List<Transform> { transform };

            foreach (var root in roots)
            {
                if (root == null)
                {
                    continue;
                }

                var components = root.GetComponentsInChildren<EquipmentSlotComponent>(true);
                foreach (var component in components)
                {
                    var slotGO = component.gameObject;

                    var button = slotGO.GetComponentInChildren<Button>(true);
                    var view = slotGO.GetComponentInChildren<ItemViewWidget>(true);

                    if (button == null)
                    {
                        Debug.LogWarning($"[EquipmentUI] No Button found under '{component.name}'.", component);
                    }

                    if (view == null)
                    {
                        Debug.LogWarning($"[EquipmentUI] No ItemViewWidget found under '{component.name}'.", component);
                    }
                    else
                    {
                        if (button != null)
                        {
                            view.SetSelectableTintTarget(button);
                        }

                        if (rarityColorSettings != null)
                        {
                            view.SetRaritySettings(rarityColorSettings);
                        }

                        view.Clear();
                    }

                    Slots.Add(new SlotBinding
                    {
                        button = button,
                        slotComponent = component,
                        itemView = view
                    });
                }
            }
        }

    }
}
