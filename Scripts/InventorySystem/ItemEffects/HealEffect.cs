using UnityEngine;

namespace JG.Inventory
{
    /// <summary>Instantly restores hit points.</summary>
    [ItemEffect("Heal")]
    public class HealEffect : IItemEffect
    {
        readonly int amount;

        public HealEffect(int amount) => this.amount = amount;

        public void Apply(IInventoryContext ctx)
        {
            if (!ctx.TryGet<Stats>(out var stats)) return;


            var healthDef = StatRegistryProvider.Instance.Registry.Get("health");
            var mod = new StatModifier(healthDef, new AddOperation(amount), 0f);
            stats.Mediator.AddModifier(mod);
            // duration 0 → removed next frame by mediator; acts as an “impulse”.

        }

        public void Remove(IInventoryContext ctx) { /* no-op */ }

        public static IItemEffect FromJson(string json)
        {
            var p = JsonUtility.FromJson<Params>(json);
            return new HealEffect(p.amount);
        }

        static HealEffect() => ItemEffectRegistry.Register<HealEffect>(FromJson);

        [System.Serializable] struct Params { public int amount; }
    }
}
