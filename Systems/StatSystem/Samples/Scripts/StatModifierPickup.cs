using UnityEngine;

namespace JG.Samples
{
    public class StatModifierPickup : Pickup
    {
        [SerializeField] private string statKey;
        [SerializeField] private OperatorType operatorType = OperatorType.Add;
        [SerializeField] private float value;
        [SerializeField, Tooltip("0 = permanent")] private float duration;

        public override void ApplyPickupEffect(Entity entity)
        {
            IOperationStrategy strategy = operatorType switch
            {
                OperatorType.Add => new AddOperation(value),
                OperatorType.Multiply => new MultiplyOperation(value),
                OperatorType.Percentage => new PercentageOperation(value),
                _ => new AddOperation(value),
            };
            entity.Stats.Mediator.AddModifier(new StatModifier(statKey, strategy, duration));
        }
    }
}
