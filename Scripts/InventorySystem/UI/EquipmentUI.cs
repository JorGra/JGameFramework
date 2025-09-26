using JGameFramework.UI.Tooltips;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

        public EquipmentHub hub;

        private readonly Dictionary<EquipmentSlotComponent, Action> slotChangedHandlers = new();

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
                }
                catch (MissingReferenceException)
                {
                    // Slot or component already destroyed; nothing to clean up.
                }
            }

            slotChangedHandlers.Clear();
        }


        private void Refresh(SlotBinding binding)
        {
            var equipped = binding.slotComponent.Slot.Equipped;
            bool hasItem = equipped != null;

            if (!hasItem)
            {
                tooltipController?.CloseContextMenu(binding.slotComponent);
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


