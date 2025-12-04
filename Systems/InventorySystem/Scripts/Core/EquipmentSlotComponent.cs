using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace JG.Inventory
{
    /// <summary>
    /// MonoBehaviour that wraps a single <see cref="EquipmentSlot"/> and
    /// lets you edit accepted equip tags in the Inspector.
    /// </summary>
    public class EquipmentSlotComponent : MonoBehaviour
    {
        [Tooltip("Any item that contains at least ONE of these tags can be equipped here.")]
        [SerializeField] private List<string> acceptedTags = new() { "Head" };

        [Tooltip("Logical category id for grouping slots (e.g., \"Primary\", \"Secondary\", \"Head\").")]
        [FormerlySerializedAs("slotCategory")]
        [SerializeField] private string slotCategoryId = "Primary";

        public string SlotCategoryId => string.IsNullOrWhiteSpace(slotCategoryId) ? "Primary" : slotCategoryId;

        public EquipmentSlot Slot { get; private set; }

        public void SetSlot() => Slot = new EquipmentSlot(acceptedTags);

        public void AdoptSlot(EquipmentSlot slot)
        {
            Slot = slot ?? new EquipmentSlot(acceptedTags);
        }

        public void EnsureSlot()
        {
            if (Slot == null)
                Slot = new EquipmentSlot(acceptedTags);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(slotCategoryId))
            {
                slotCategoryId = "Primary";
            }
        }
    }
}
