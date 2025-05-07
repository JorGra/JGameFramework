using UnityEngine;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Tries to equip a stack into the first compatible EquipmentSlotComponent.
    /// </summary>
    public class PlayerEquipmentBridge : MonoBehaviour
    {
        [SerializeField] Transform slotContainer;
        [SerializeField] private EquipmentSlotComponent[] slots;

        bool AwakeCalled;
        void Awake()
        {
            if (slots.Length == 0)
                slots = slotContainer.GetComponentsInChildren<EquipmentSlotComponent>(true);
            AwakeCalled = true;
        }

        public bool Equip(ItemStack stack)
        {
            if (!AwakeCalled) Awake();      // in case called before Awake
            foreach (var s in slots)
                if (s.Slot.CanEquip(stack.Data))
                    return s.Slot.Equip(stack, new InventoryContext());
            return false;
        }
    }
}
