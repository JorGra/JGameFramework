using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory.Samples
{
    /// <summary>
    /// Test utility: press the configured key to add one random item (by id)
    /// to the assigned inventory at runtime.
    /// </summary>
    public class RandomItemGiver : MonoBehaviour
    {
        [Header("Target Inventory (assign in Inspector)")]
        [Tooltip("Any MonoBehaviour that exposes an Inventory via the Runtime property.")]
        [SerializeField] private PassiveInventoryComponent inventory;   // assign regular bag
        // To test the passive bag instead, drag a PassiveInventoryComponent here.

        [Header("Item Ids (catalog keys)")]
        [SerializeField] private List<string> itemIds = new();

        [Header("Input")]
        [SerializeField] private KeyCode triggerKey = KeyCode.R;

        void Update()
        {
            if (!Input.GetKeyDown(triggerKey) || itemIds.Count == 0) return;

            string id = itemIds[Random.Range(0, itemIds.Count)];
            ItemData data = ItemCatalog.Instance.Get(id);

            if (data == null)
            {
                Debug.LogWarning($"[RandomItemGiver] Item id '{id}' not found.");
                return;
            }

            if (inventory == null)
            {
                Debug.LogWarning("[RandomItemGiver] No InventoryComponent assigned.");
                return;
            }

            inventory.Runtime.AddItem(data, 1);
        }
    }
}
