using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryComponent playerInventory;   // NEW
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private ItemSlotUI slotPrefab;

        [Header("Context Menu")]
        [SerializeField] private ContextMenuUI contextPrefab;

        private ContextMenuUI context;
        private readonly List<ItemSlotUI> pool = new();

        void Awake()
        {
            if (playerInventory == null)
                playerInventory = FindObjectOfType<InventoryComponent>();

            if (playerInventory == null)
            {
                Debug.LogError("InventoryUI: No InventoryComponent assigned or found.");
                enabled = false; return;
            }

            playerInventory.Runtime.Changed += Rebuild;
            Rebuild();
        }

        public void ShowContextMenu(ItemStack stack, RectTransform anchor)
        {
            context ??= Instantiate(contextPrefab, transform.root);
            context.Open(stack, playerInventory.Runtime, anchor, this);
        }

        void Rebuild()
        {
            var inv = playerInventory.Runtime;
            int need = inv.Slots.Count;

            while (pool.Count < need)
                pool.Add(Instantiate(slotPrefab, contentRoot));

            for (int i = 0; i < pool.Count; i++)
            {
                bool active = i < need;
                pool[i].gameObject.SetActive(active);
                if (active)
                    pool[i].Init(inv.Slots[i].Stack, this);
            }
        }
    }
}
