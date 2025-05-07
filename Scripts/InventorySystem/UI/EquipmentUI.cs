using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.Inventory.UI
{
    public class EquipmentUI : MonoBehaviour
    {
        [System.Serializable]
        private struct SlotBinding
        {
            public Button button;
            public EquipmentSlotComponent slotComponent; // NEW
            public Image icon;
            public TMP_Text qty;
        }

        [SerializeField] private List<SlotBinding> slots;

        void Awake()
        {
            foreach (var b in slots)
            {
                if (b.slotComponent == null)
                {
                    Debug.LogError($"{name}: slot binding missing component");
                    continue;
                }

                b.slotComponent.Slot.Changed += () => Refresh(b);
                Refresh(b);
            }
        }

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
