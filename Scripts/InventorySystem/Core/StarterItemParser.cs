using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Static helper that converts one or more starter-item JSON files into
    /// tuples of <c>(ItemData, quantity)</c>.  
    ///
    /// Accepted formats (same as <see cref="InventoryComponent"/> legacy):
    /// <list type="bullet">
    /// <item><description>Single-item file  
    /// <code>{ "id":"potion_health", "quantity":5 }</code></description></item>
    /// <item><description>Multi-item wrapper  
    /// <code>{ "items":[ { … }, { … } ] }</code></description></item>
    /// </list>
    /// Unknown IDs are ignored with a logged warning.
    /// </summary>
    public static class StarterItemParser
    {
        /// <summary>Parses one TextAsset.</summary>
        public static IEnumerable<(ItemData data, int qty)> Parse(TextAsset ta)
        {
            if (ta == null) yield break;

            string json = ta.text;

            // 1) wrapper variant?
            var multi = JsonUtility.FromJson<ItemSeedFile>(json);
            if (multi != null && multi.items != null && multi.items.Count > 0)
            {
                foreach (var seed in multi.items)
                    if (TryResolve(seed, out var pair)) yield return pair;
                yield break;
            }

            // 2) single-object variant
            var single = JsonUtility.FromJson<ItemSeed>(json);
            if (!string.IsNullOrWhiteSpace(single.id) &&
                TryResolve(single, out var singlePair))
                yield return singlePair;
        }

        /// <summary>Convenience wrapper to iterate many files at once.</summary>
        public static IEnumerable<(ItemData data, int qty)>
            ParseMany(IEnumerable<TextAsset> files)
        {
            if (files == null) yield break;
            foreach (var ta in files)
                foreach (var tuple in Parse(ta))
                    yield return tuple;
        }

        /* ───────── helpers ───────── */

        static bool TryResolve(ItemSeed seed, out (ItemData, int) tuple)
        {
            tuple = default;
            var data = ItemCatalog.Instance.Get(seed.id);
            if (data == null)
            {
                Debug.LogWarning(
                    $"[StarterItemParser] Item id '{seed.id}' not found in catalog.");
                return false;
            }
            tuple = (data, Math.Max(1, seed.quantity));
            return true;
        }

        /* ───────── DTOs ───────── */

        [Serializable]
        struct ItemSeed
        {
            public string id;
            public int quantity;
        }

        [Serializable]
        class ItemSeedFile
        {
            public List<ItemSeed> items = new();
        }
    }
}
