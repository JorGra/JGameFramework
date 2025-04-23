using UnityEngine;
using UnityServiceLocator;

namespace JG.Samples
{
    public class Entity : MonoBehaviour
    {
        /// <summary>
        /// The runtime Stats container (no profile needed).
        /// </summary>
        public Stats Stats { get; private set; }

        private IStatModifierFactory modifierFactory;

        void Awake()
        {
            // Construct Stats: pulls all defaults from your master JSON.
            Stats = new Stats();

            // Create the factory (or resolve via your DI/ServiceLocator)
            modifierFactory = ServiceLocator.For(this).Get<IStatModifierFactory>();
        }

        void Start()
        {
            // 1) Lookup your stat definition by key
            var powerDef = StatRegistryProvider.Instance.Registry.Get("power");
            // 2) Direct modifier: +10 for 5 seconds
            var directMod = new StatModifier(powerDef, new AddOperation(10f), 5f);
            Stats.Mediator.AddModifier(directMod);
            Debug.Log($"Power after direct modifier: {Stats.GetStat(powerDef)}");

            // 3) Factory modifier: +20 permanently
            var factoryMod = modifierFactory.Create(powerDef, OperatorType.Add, 20f, 0f);
            Stats.Mediator.AddModifier(factoryMod);
            Debug.Log($"Power after factory modifier: {Stats.GetStat(powerDef)}");
        }

        void Update()
        {
            // Ticks modifier durations and cleans up expired ones
            Stats.Mediator.Update(Time.deltaTime);
        }
    }
}
