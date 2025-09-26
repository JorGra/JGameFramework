using JGameFramework.UI.Tooltips;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using JG.GameContent;

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
            public Image icon;
            public TMP_Text qty;
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
                        bool canUse = equipped.Data.Effects != null && equipped.Data.Effects.Count > 0;

                        if (canUse)
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
                        callback: (handle, ctx) => handle.Close()));
                },
                eventData: null,
                contextOverride: null,
                followTarget: false);
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
            bool hasItem = equipped != null;

            if (!hasItem)
            {
                tooltipController?.CloseContextMenu(binding.slotComponent);
                HideSlotTooltip(binding.slotComponent);
            }

            if (binding.icon != null)
            {
                binding.icon.enabled = hasItem;
                binding.icon.sprite = hasItem ? equipped.Data.Icon : null;
            }

            if (binding.qty != null)
            {
                binding.qty.text = hasItem && equipped.Count > 1 ? equipped.Count.ToString() : string.Empty;
            }
        }

        //public void RebuildBindings()(SlotBinding binding)
        //{
        //    var equipped = binding.slotComponent.Slot.Equipped;
        //    bool hasItem = equipped != null;

        //    if (!hasItem)
        //    {
        //        tooltipController?.CloseContextMenu(binding.slotComponent);
        //    }

        //    if (binding.icon != null)
        //    {
        //        binding.icon.enabled = hasItem;
        //        binding.icon.sprite = hasItem ? equipped.Data.Icon : null;
        //    }

        //    if (binding.qty != null)
        //    {
        //        binding.qty.text = hasItem && equipped.Count > 1 ? equipped.Count.ToString() : string.Empty;
        //    }
        //}

        private void ConfigureTooltip(SlotBinding binding)
        {
            var slotComponent = binding.slotComponent;
            if (slotComponent == null)
            {
                return;
            }

            RemoveTooltipBinding(slotComponent);

            if (!showSlotTooltips)
            {
                return;
            }

            var targetGO = binding.button != null ? binding.button.gameObject : slotComponent.gameObject;
            if (targetGO == null)
            {
                return;
            }

            var trigger = targetGO.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = targetGO.AddComponent<EventTrigger>();
            }

            var capturedBinding = binding;
            var pointerEnter = AddTriggerEntry(trigger, EventTriggerType.PointerEnter, data => ShowSlotTooltip(capturedBinding, data));
            var pointerExit = AddTriggerEntry(trigger, EventTriggerType.PointerExit, data => HideSlotTooltip(capturedBinding.slotComponent));
            var select = AddTriggerEntry(trigger, EventTriggerType.Select, data => ShowSlotTooltip(capturedBinding, data));
            var deselect = AddTriggerEntry(trigger, EventTriggerType.Deselect, data => HideSlotTooltip(capturedBinding.slotComponent));

            tooltipBindings[slotComponent] = new SlotTooltipBinding
            {
                trigger = trigger,
                pointerEnter = pointerEnter,
                pointerExit = pointerExit,
                select = select,
                deselect = deselect
            };
        }

        private EventTrigger.Entry AddTriggerEntry(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> callback)
        {
            if (trigger == null || callback == null)
            {
                return null;
            }

            if (trigger.triggers == null)
            {
                trigger.triggers = new List<EventTrigger.Entry>();
            }

            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
            return entry;
        }

        private void RemoveTooltipBinding(EquipmentSlotComponent slotComponent)
        {
            if (slotComponent == null)
            {
                return;
            }

            if (!tooltipBindings.TryGetValue(slotComponent, out var binding))
            {
                return;
            }

            RemoveTriggerEntry(binding.trigger, binding.pointerEnter);
            RemoveTriggerEntry(binding.trigger, binding.pointerExit);
            RemoveTriggerEntry(binding.trigger, binding.select);
            RemoveTriggerEntry(binding.trigger, binding.deselect);
            tooltipBindings.Remove(slotComponent);
        }

        private static void RemoveTriggerEntry(EventTrigger trigger, EventTrigger.Entry entry)
        {
            if (trigger == null || entry == null)
            {
                return;
            }

            if (trigger.triggers != null)
            {
                trigger.triggers.Remove(entry);
            }
        }

        private void ShowSlotTooltip(SlotBinding binding, BaseEventData eventData)
        {
            if (!showSlotTooltips || tooltipController == null)
            {
                return;
            }

            var slotComponent = binding.slotComponent;
            if (!TryGetEquippedItem(slotComponent, out var itemDef))
            {
                HideSlotTooltip(slotComponent);
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

        private void HideSlotTooltip(EquipmentSlotComponent slotComponent)
        {
            if (slotComponent == null)
            {
                return;
            }

            tooltipController?.CloseTooltip(slotComponent);
        }

        private RectTransform ResolveTooltipAnchor(SlotBinding binding)
        {
            if (binding.button != null)
            {
                return binding.button.transform as RectTransform;
            }

            return binding.slotComponent != null ? binding.slotComponent.GetComponent<RectTransform>() : null;
        }

        private bool TryGetEquippedItem(EquipmentSlotComponent slotComponent, out ItemDef itemDef)
        {
            itemDef = null;

            var slot = slotComponent?.Slot;
            var equipped = slot?.Equipped;
            if (equipped == null || equipped.Data == null)
            {
                return false;
            }

            if (equipped.Data is ItemDef directDef)
            {
                itemDef = directDef;
                return true;
            }

            if (ContentCatalogue.Instance.TryGet(equipped.Data.Id, out itemDef))
            {
                return itemDef != null;
            }

            return false;
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
                if (root == null) continue;

                var components = root.GetComponentsInChildren<EquipmentSlotComponent>(true);
                foreach (var component in components)
                {
                    var slotGO = component.gameObject;

                    var button = slotGO.GetComponentInChildren<Button>(true);

                    Image icon = null;
                    var images = slotGO.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        if (button != null && img.transform.IsChildOf(button.transform)) continue;
                        if (img.transform.parent == slotGO.transform)
                        {
                            icon = img;
                            break;
                        }
                    }
                    if (icon == null)
                    {
                        foreach (var img in images)
                        {
                            if (button != null && img.transform.IsChildOf(button.transform)) continue;
                            icon = img;
                            break;
                        }
                    }

                    TMP_Text qty = null;
                    if (button != null)
                    {
                        qty = button.GetComponentInChildren<TMP_Text>(true);
                    }

                    if (button == null)
                        Debug.LogWarning($"[EquipmentUI] No Button found under '{component.name}'.", component);
                    if (icon == null)
                        Debug.LogWarning($"[EquipmentUI] No Image (icon) found under '{component.name}'.", component);
                    if (qty == null)
                        Debug.LogWarning($"[EquipmentUI] No TMP_Text (qty) under the Button in '{component.name}'.", component);

                    Slots.Add(new SlotBinding
                    {
                        button = button,
                        slotComponent = component,
                        icon = icon,
                        qty = qty
                    });
                }
            }
        }
    }
}


