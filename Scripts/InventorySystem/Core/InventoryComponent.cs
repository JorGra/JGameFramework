using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Holds the player's <see cref="Inventory"/> and can seed it from one or
    /// more JSON TextAssets.  Each file may contain:
    ///
    /// • exactly ONE object  →  { "id":"potion", "quantity":3 }  
    /// • or MANY objects     →  { "items":[ {…}, {…} ] }
    /// </summary>
    public class InventoryComponent : MonoBehaviour
    {
        public Inventory Runtime { get; private set; }

        [Header("Starter Item Files (TextAssets)")]
        [SerializeField] private List<TextAsset> starterFiles = new();

        void Awake()
        {
            Runtime = new Inventory();

            foreach (var ta in starterFiles)
                if (ta != null) LoadFile(ta.text);
        }

        /* ───────────────────────── helpers ───────────────────────── */

        void LoadFile(string json)
        {
            // 1) multi-item wrapper?
            var multi = JsonUtility.FromJson<ItemSeedFile>(json);
            if (multi != null && multi.items != null && multi.items.Count > 0)
            {
                foreach (var s in multi.items)
                    AddSeed(s);
                return;
            }

            // 2) single object format
            var single = JsonUtility.FromJson<ItemSeed>(json);
            if (!string.IsNullOrWhiteSpace(single.id))
                AddSeed(single);
        }

        void AddSeed(ItemSeed s)
        {
            var data = ItemCatalog.Instance.Get(s.id);
            if (data != null)
                Runtime.AddItem(data, Mathf.Max(1, s.quantity));
            else
                Debug.LogWarning($"[InventoryComponent] Item id '{s.id}' not found in catalog.");
        }

        /* ───────────────────────── DTOs ───────────────────────── */

        [System.Serializable]
        private struct ItemSeed
        {
            public string id;
            public int quantity;
        }

        [System.Serializable]
        private class ItemSeedFile
        {
            public List<ItemSeed> items = new();
        }
    }
}
