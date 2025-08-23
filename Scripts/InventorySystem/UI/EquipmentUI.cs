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

        [Header("Bindings")] public List<SlotBinding> Slots = new();
        [Header("Context-menu setup")][SerializeField] private ContextMenuUI contextPrefab;
        //public InventoryComponent playerInventory;
        public EquipmentHub hub;
        ContextMenuUI context;

        public void Init()
        {
            foreach (var raw in Slots)
            {
                var b = raw;                                    // capture!
                if (b.slotComponent == null)
                {
                    Debug.LogError($"{name}: slot binding missing component");
                    continue;
                }
                b.button.onClick.RemoveAllListeners();
                b.button.onClick.AddListener(() => OnSlotClicked(b));
                b.slotComponent.EnsureSlot();
                var slotRef = b.slotComponent.Slot;
                slotRef.Changed += () => Refresh(b);
                Refresh(b);
            }

            hub ??= GetComponentInParent<EquipmentHub>(true);
        }

        /* ---------- click / context ---------- */

        void OnSlotClicked(SlotBinding b)
        {
            var eq = b.slotComponent.Slot.Equipped;
            if (eq == null) return;

            context ??= Instantiate(contextPrefab, transform.root);
            context.Open(eq,                                             // stack
                         hub.Inventory,                        // inv
                         (RectTransform)b.button.transform,              // anchor
                         hub,
                         b.slotComponent);                               // signals “equipped”
        }

        /* ---------- visual refresh ---------- */

        void Refresh(SlotBinding b)
        {
            Debug.Log($"[EquipmentUI] Refresh {b.slotComponent.name}");
            var eq = b.slotComponent.Slot.Equipped;
            bool hasItem = eq != null;

            b.icon.enabled = hasItem;
            b.icon.sprite = hasItem ? eq.Data.Icon : null;
            b.qty.text = hasItem && eq.Count > 1 ? eq.Count.ToString() : "";
        }

        void OnEnable()
        {
            foreach (var raw in Slots)
            {
                var b = raw;
                if (b.slotComponent?.Slot != null) Refresh(b);
            }
        }

    }
}
