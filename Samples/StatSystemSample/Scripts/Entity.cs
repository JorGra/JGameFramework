using UnityEngine;

namespace JG.Samples
{
    public class Entity : MonoBehaviour
    {
        [SerializeField] private StatsProfile statsProfile;
        public Stats Stats { get; private set; }

        private void Awake()
        {
            // Construct the Stats object from the assigned UnitStatsProfile.
            Stats = new Stats(statsProfile);
        }

        private void Start()
        {
            // Direct usage: add a modifier to the MaxHealth stat.
            Stats.Mediator.AddModifier(new StatModifier(GameStatDefinitions.MaxHealth, new AddOperation(10), 10f));
            Debug.Log("Max health after direct modifier: " + Stats.GetStat(GameStatDefinitions.MaxHealth));

            // Using ServiceLocator to get the modifier factory:
            var modifierFactory = UnityServiceLocator.ServiceLocator.For(this).Get<IStatModifierFactory>();
            Stats.Mediator.AddModifier(modifierFactory.Create(GameStatDefinitions.MaxHealth, OperatorType.Add, 10f, 0f));
            Debug.Log("Max health after factory modifier: " + Stats.GetStat(GameStatDefinitions.MaxHealth));
        }

        private void Update()
        {
            // Update modifiers each frame.
            Stats.Mediator.Update(Time.deltaTime);
        }
    }
}
