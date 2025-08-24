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

        [Header("Context-menu setup")]
        [SerializeField] private ContextMenuUI contextPrefab;

        public EquipmentHub hub;
        private ContextMenuUI context;

        [ContextMenu("Auto-bind + Init")]
        private void AutoBindAndInit()
        {
            RebuildBindings();
            Init();
        }

        [ContextMenu("Auto-bind (no Init)")]
        private void AutoBindOnly() => RebuildBindings();

        public void Init()
        {
            hub ??= GetComponentInParent<EquipmentHub>(true);

            // if someone forgot to auto-bind in editor, do it now
            if (Slots.Count == 0) RebuildBindings();

            foreach (var raw in Slots)
            {
                var b = raw; // capture

                if (b.slotComponent == null)
                {
                    Debug.LogWarning($"{name}: Slot binding missing EquipmentSlotComponent.", this);
                    continue;
                }

                if (b.button != null)
                {
                    b.button.onClick.RemoveAllListeners();
                    b.button.onClick.AddListener(() => OnSlotClicked(b));
                }

                b.slotComponent.EnsureSlot();
                var slotRef = b.slotComponent.Slot;
                slotRef.Changed += () => Refresh(b);
                Refresh(b);
            }
        }

        void OnEnable()
        {
            foreach (var raw in Slots)
            {
                var b = raw;
                if (b.slotComponent?.Slot != null) Refresh(b);
            }
        }

        /* ---------- click / context ---------- */

        void OnSlotClicked(SlotBinding b)
        {
            var eq = b.slotComponent.Slot.Equipped;
            if (eq == null || b.button == null) return;

            context ??= Instantiate(contextPrefab, transform.root);
            context.Open(eq,
                         hub.Inventory,
                         (RectTransform)b.button.transform,
                         hub,
                         b.slotComponent);
        }

        /* ---------- visual refresh ---------- */

        void Refresh(SlotBinding b)
        {
            var eq = b.slotComponent.Slot.Equipped;
            bool hasItem = eq != null;

            if (b.icon != null)
            {
                b.icon.enabled = hasItem;
                b.icon.sprite = hasItem ? eq.Data.Icon : null;
            }

            if (b.qty != null)
                b.qty.text = hasItem && eq.Count > 1 ? eq.Count.ToString() : "";
        }

        /* ---------- auto-binding (simple fixed layout) ---------- */

        /// <summary>
        /// Find all EquipmentSlotComponent under the given containers and bind:
        /// - Button: first Button under the slot
        /// - Icon:   Image directly under the slot (not inside the Button)
        /// - Qty:    TMP_Text under the Button
        /// </summary>
        public void RebuildBindings()
        {
            Slots.Clear();

            var roots = slotContainers != null && slotContainers.Count > 0
                ? slotContainers
                : new List<Transform> { transform };

            foreach (var root in roots)
            {
                if (root == null) continue;

                var comps = root.GetComponentsInChildren<EquipmentSlotComponent>(true);
                foreach (var comp in comps)
                {
                    var slotGO = comp.gameObject;

                    // 1) Button: first Button under the slot
                    var button = slotGO.GetComponentInChildren<Button>(true);

                    // 2) Icon Image: prefer an Image that is a direct child of the slot and NOT under the Button
                    Image icon = null;
                    var images = slotGO.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        if (button != null && img.transform.IsChildOf(button.transform)) continue;
                        if (img.transform.parent == slotGO.transform) { icon = img; break; }
                    }
                    // fallback: if we didn’t find a direct child, allow any image not under the button
                    if (icon == null)
                    {
                        foreach (var img in images)
                        {
                            if (button != null && img.transform.IsChildOf(button.transform)) continue;
                            icon = img;
                            break;
                        }
                    }

                    // 3) Qty text: first TMP_Text under the Button
                    TMP_Text qty = null;
                    if (button != null)
                        qty = button.GetComponentInChildren<TMP_Text>(true);

                    if (button == null)
                        Debug.LogWarning($"[EquipmentUI] No Button found under '{comp.name}'.", comp);
                    if (icon == null)
                        Debug.LogWarning($"[EquipmentUI] No Image (icon) found under '{comp.name}'.", comp);
                    if (qty == null)
                        Debug.LogWarning($"[EquipmentUI] No TMP_Text (qty) under the Button in '{comp.name}'.", comp);

                    Slots.Add(new SlotBinding
                    {
                        button = button,
                        slotComponent = comp,
                        icon = icon,
                        qty = qty
                    });
                }
            }
        }
    }
}
