using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Generic key-value container shared by inventory & resource wallets.
    /// </summary>
    public abstract class Container<TKey, TValue>
    {
        protected readonly Dictionary<TKey, TValue> entries = new();

        public IReadOnlyDictionary<TKey, TValue> Entries => entries;

        public bool TryGet(TKey key, out TValue value) => entries.TryGetValue(key, out value);

        public void Clear() => entries.Clear();
    }
}
