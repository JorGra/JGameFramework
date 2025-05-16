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
    public class PassiveInventoryComponent : MonoBehaviour
    {
        /// <summary>Runtime container with auto-equip behaviour.</summary>
        public Inventory Runtime { get; private set; }

        [Header("Starter Item Files (TextAssets)")]
        [SerializeField] List<TextAsset> starterFiles = new();

        void Awake()
        {
            var statsProv = GetComponent<IStatsProvider>() ??
                            GetComponentInParent<IStatsProvider>();

            Runtime = new Inventory(statsProv, new PassiveEquipHook());

            StartCoroutine(AddStarterItems());
        }

        public IEnumerator AddStarterItems()
        {
            yield return null;

            foreach (var (data, qty) in StarterItemParser.ParseMany(starterFiles))
                Runtime.AddItem(data, qty);

        }
        [ContextMenu("Print All Items")]
        void PrintAllItems()
        {
            foreach (var slot in Runtime.Slots)
            {
                Debug.Log($"Slot: {slot.Stack.Data.DisplayName}");
            }
        }
    }
}
