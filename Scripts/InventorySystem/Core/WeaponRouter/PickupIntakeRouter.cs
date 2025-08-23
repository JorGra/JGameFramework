using UnityEngine;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Routes incoming items: weapons → weapon slots; everything else → endless bag.
    /// If a weapon cannot be placed (no free slot), the pickup fails.
    /// </summary>
    public class PickupIntakeRouter : MonoBehaviour
    {
        [SerializeField] private InventoryComponent bag;       // your endless inventory. :contentReference[oaicite:16]{index=16} :contentReference[oaicite:17]{index=17}
        public WeaponSlotRouter weapons;

        public bool TryAdd(IInventoryItem item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            for (int i = 0; i < quantity; i++)
            {
                if (weapons.IsWeapon(item))
                {
                    if (!weapons.TryAddWeapon(item))
                        return false; // strict: no slot available → no pickup
                }
                else
                {
                    if (!bag.Runtime.AddItem(item, 1))
                        return false;
                }
            }
            return true;
        }
    }
}
