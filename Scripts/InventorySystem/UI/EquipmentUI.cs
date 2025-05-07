using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Displays all player equipment slots and lets the user open a context-menu
    /// (right-click or pad “Y/△”) on occupied slots.
    /// </summary>
    public class EquipmentUI : MonoBehaviour
    {
        [System.Serializable]
        private struct SlotBinding
        {
            public Button button;
            public EquipmentSlotComponent slotComponent;
            public Image icon;
            public TMP_Text qty;
        }

        [Header("Bindings")]
        [SerializeField] private List<SlotBinding> slots = new();

        [Header("Context-menu setup")]
        [SerializeField] private ContextMenuUI contextPrefab;
        [SerializeField] private InventoryComponent playerInventory;      // owner

        ContextMenuUI context;

        void Awake()
        {
            foreach (var b in slots)
            {
                if (b.slotComponent == null) { Debug.LogError($"{name}: slot binding missing component"); continue; }

                b.button.onClick.AddListener(() => OnSlotClicked(b));     // game-pad / LMB
                b.slotComponent.Slot.Changed += () => Refresh(b);
                Refresh(b);
            }

            if (playerInventory == null)
                playerInventory = FindObjectOfType<InventoryComponent>(true);
        }

        /* ───────── click / context ───────── */

        void OnSlotClicked(SlotBinding b)
        {
            var eq = b.slotComponent.Slot.Equipped;
            if (eq == null) return;                                      // empty

            context ??= Instantiate(contextPrefab, transform.root);
            context.Open(eq,                                             // stack
                         playerInventory.Runtime,                        // inventory
                         b.button.transform as RectTransform,            // anchor
                                                                         //TODO: FIX THIS CAUSING ISSUES ON UNEQUIP
                         playerInventory.GetComponent<PlayerEquipmentBridge>(),                                           // no InventoryUI owner 
                         b.slotComponent);                               // signals "equipped"
        }

        /* ───────── visual refresh ───────── */

        void Refresh(SlotBinding b)
        {
            var eq = b.slotComponent.Slot.Equipped;
            bool hasItem = eq != null;

            b.icon.enabled = hasItem;
            b.icon.sprite = hasItem ? eq.Data.Icon : null;
            b.qty.text = hasItem && eq.Count > 1 ? eq.Count.ToString() : "";
        }
    }
}
