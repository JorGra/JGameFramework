using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Attach to any entity that needs a “passive-bonus” inventory.
    /// Uses <see cref="StarterItemParser"/> for seeding and
    /// <see cref="PassiveEquipHook"/> to auto-apply equip effects.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PassiveInventoryComponent : MonoBehaviour, IInventoryHolder
    {
        /// <summary>Runtime container with auto-equip behaviour.</summary>
        private Inventory Runtime { get; set; }

        [Header("Starter Item Files (TextAssets)")]
        [SerializeField] List<TextAsset> starterFiles = new();

        void Awake()
        {
            StartCoroutine(AddStarterItems());
        }

        public IEnumerator AddStarterItems()
        {
            yield return null;

            foreach (var (data, qty) in StarterItemParser.ParseMany(starterFiles))
                Get().AddItem(data, qty);

        }
        [ContextMenu("Print All Items")]
        void PrintAllItems()
        {
            foreach (var slot in Runtime.Slots)
            {
                Debug.Log($"Slot: {slot.Stack.Data.DisplayName}");
            }
        }

        public Inventory Get()
        {
            if (Runtime == null)
            {
                var statsProv = GetComponent<IStatsProvider>() ??
                 GetComponentInParent<IStatsProvider>();

                Runtime = new Inventory(null, new PassiveEquipHook());
            }
            return Runtime;
        }
    }
}
