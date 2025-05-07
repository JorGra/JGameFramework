using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Scroll-view-based inventory list for a single player.
    /// Keeps item slots and tooltip panel in sync with the runtime inventory.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryComponent playerInventory;
        [SerializeField] private PlayerEquipmentBridge playerBridge;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private ItemSlotUI slotPrefab;
        [Tooltip("Tooltip panel that belongs to THIS inventory window.")]
        [SerializeField] private ItemDetailPanelUI detailPanel;

        [Header("Context Menu")]
        [SerializeField] private ContextMenuUI contextPrefab;

        readonly List<ItemSlotUI> pool = new();
        ContextMenuUI context;

        void Awake()
        {
            if (playerInventory == null)
                playerInventory = GetComponentInParent<InventoryComponent>();

            if (playerInventory == null)
            {
                Debug.LogError($"{name}: No InventoryComponent assigned or found.");
                enabled = false; return;
            }
            if (playerBridge == null)
                playerBridge = GetComponentInParent<PlayerEquipmentBridge>();


            playerInventory.Runtime.Changed += Rebuild;
        }

        void OnEnable() => Rebuild();
        void OnDisable() => context?.Close();

        /// <summary>Called by <see cref="ItemSlotUI"/> to open the context-menu.</summary>
        public void ShowContextMenu(ItemStack stack, RectTransform anchor)
        {
            if (stack == null) return;

            context ??= Instantiate(contextPrefab, transform.root);
            context.Open(stack,
                         playerInventory.Runtime,
                         anchor,
                         playerBridge);                     // pass bridge ✔
        }

        /* ───────── build / refresh ───────── */

        void Rebuild()
        {
            var inv = playerInventory.Runtime;
            if (inv == null) return;

            int needed = inv.Slots.Count;

            while (pool.Count < needed)
                pool.Add(Instantiate(slotPrefab, contentRoot));

            for (int i = 0; i < pool.Count; i++)
            {
                bool active = i < needed;
                pool[i].gameObject.SetActive(active);

                if (active)
                    pool[i].Init(inv.Slots[i].Stack, this, detailPanel);
            }
        }
    }
}
