using JG.Inventory;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Restores hit points to the player or target entity.
    /// </summary>
    public class HealEffect : IItemEffect
    {
        private readonly int amount;

        public HealEffect(int amount)
        {
            this.amount = amount;
        }

        public void Apply(InventoryContext context)
        {
            //context.TargetStats?.RestoreHealth(amount);
        }

        public void Remove(InventoryContext context)
        {
            /* no-op: healing has no persistent removal */
        }

        /// <summary>
        /// Factory helper used by <see cref="ItemEffectFactory"/>.
        /// </summary>
        public static IItemEffect CreateFromJson(string json)
        {
            var data = JsonUtility.FromJson<HealParams>(json);
            return new HealEffect(data.amount);
        }

        [System.Serializable]
        private struct HealParams { public int amount; }
    }
}
