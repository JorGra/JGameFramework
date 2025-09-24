using Weapons;
using System.Collections.Generic;
using UnityEngine;

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

        [Tooltip("Determines which weapon group this slot belongs to (used for auto-equip/combine).")]
        [SerializeField] private WeaponSlotCategory slotCategory = WeaponSlotCategory.Primary;

        public WeaponSlotCategory SlotCategory => slotCategory;

        public EquipmentSlot Slot { get; private set; }

        public void SetSlot() => Slot = new EquipmentSlot(acceptedTags);

        public void EnsureSlot()
        {
            if (Slot == null)
                Slot = new EquipmentSlot(acceptedTags);
        }


    }
}

