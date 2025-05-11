using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory.UI
{
    /// <summary>
    /// Scroll-view list for the “passive-bonus” inventory. Keeps item slots,
    /// tooltip and context-menu in sync with <see cref="PassiveInventoryComponent"/>.
    /// </summary>
    public class PassiveInventoryUI : MonoBehaviour, IContextMenuHost
    {
        [Header("References")]
        [SerializeField] private PassiveInventoryComponent passiveInventory;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private ItemSlotUI slotPrefab;
        [Tooltip("Tooltip panel that belongs to THIS inventory window.")]
        [SerializeField] private ItemDetailPanelUI detailPanel;

        [Header("Context Menu")]
        [SerializeField] private ContextMenuUI contextPrefab;

        readonly List<ItemSlotUI> pool = new();
        ContextMenuUI context;

        /* ───────── initialisation ───────── */

        void Awake()
        {
            /* auto-assign if left empty */
            if (passiveInventory == null)
                passiveInventory = GetComponentInParent<PassiveInventoryComponent>();

            if (passiveInventory == null)
            {
                Debug.LogError($"{name}: No PassiveInventoryComponent assigned or found.");
                enabled = false;
                return;
            }

            passiveInventory.Runtime.Changed += Rebuild;
        }

        void OnEnable() => Rebuild();
        void OnDisable() => context?.Close();

        /* ───────── called by ItemSlotUI ───────── */

        public void ShowContextMenu(ItemStack stack, RectTransform anchor)
        {
            if (stack == null) return;

            context ??= Instantiate(contextPrefab, transform.root);
            context.Open(stack,
                         passiveInventory.Runtime,
                         (RectTransform)anchor,
                         null,                                   // no router
                         null,                                   // no equipSlot
                         GetComponentInParent<IStatsProvider>()  // supply stats provider
                        );              
        }

        /* ───────── build / refresh ───────── */

        void Rebuild()
        {
            var inv = passiveInventory.Runtime;
            if (inv == null) return;

            int needed = inv.Slots.Count;

            /* ensure enough pooled widgets */
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
