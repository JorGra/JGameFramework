using UnityEngine;

namespace JG.Inventory
{
    /// <summary>Restores hit points to the player or target entity.</summary>
    [ItemEffect("Heal")]                       
    public class HealEffect : IItemEffect
    {
        private readonly int amount;

        public HealEffect(int amount) => this.amount = amount;

        public void Apply(InventoryContext ctx)
        {
            //ctx.TargetStats?.RestoreHealth(amount);
        }

        public void Remove(InventoryContext ctx)
        {
            /* no-op: healing is instant */
        }

        /// <summary>Factory consumed by the registry.</summary>
        public static IItemEffect FromJson(string json)  
        {
            var data = JsonUtility.FromJson<HealParams>(json);
            return new HealEffect(data.amount);
        }

        /* automatic self-registration — runs once when the class is first touched */
        static HealEffect() =>
            ItemEffectRegistry.Register<HealEffect>(FromJson);

        [System.Serializable] private struct HealParams { public int amount; }
    }
}
