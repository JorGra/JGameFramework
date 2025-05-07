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

        public EquipmentSlot Slot { get; private set; }

        void Awake() =>
            Slot = new EquipmentSlot(acceptedTags);
    }
}
