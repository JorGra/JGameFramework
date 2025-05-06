using JG.Inventory;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Simple numeric resource container (wood, gold, etc.) built on <see cref="Container{TKey,TValue}"/>.
    /// </summary>
    public class ResourceWallet : Container<string, int>
    {
        public int Get(string resource) => entries.TryGetValue(resource, out int value) ? value : 0;

        public void Add(string resource, int amount)
        {
            entries[resource] = Get(resource) + Mathf.Max(0, amount);
        }

        public bool Spend(string resource, int amount)
        {
            int current = Get(resource);
            if (current < amount) return false;
            entries[resource] = current - amount;
            return true;
        }
    }
}
