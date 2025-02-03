using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Samples
{

    public class Entity : MonoBehaviour
    {
        //[SerializeField] BaseStats baseStats;
        [SerializeField] StatsDecorator[] stats;
        public Stats Stats { get; private set; }

        private void Awake()
        {
            Stats = new Stats(new StatsMediator(), stats);
        }
        // Start is called before the first frame update
        void Start()
        {
            //Direct:
            Stats.Mediator.AddModifier(new StatModifier(StatType.MaxHealth, new AddOperation(10), duration: 10f));
            Debug.Log("Max health: " + Stats.GetStat(StatType.MaxHealth));

            //using ServiceLocator:
            var modifierFactory = UnityServiceLocator.ServiceLocator.For(this).Get<IStatModifierFactory>();
            Stats.Mediator.AddModifier(modifierFactory.Create(StatType.MaxHealth, OperatorType.Add, value: 10f, duration: 0f));
            Debug.Log("Max health: " + Stats.GetStat(StatType.MaxHealth));
        }

        // Update is called once per frame
        void Update()
        {
            Stats.Mediator.Update(Time.deltaTime);
        }
    }

}