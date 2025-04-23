using System;

public enum OperatorType
{
    Add,
    Multiply,
    Percentage
}

public interface IStatModifierFactory
{
    StatModifier Create(StatDefinition statDefinition, OperatorType operatorType, float value, float duration);
    StatModifier Create(StatModifierConfig statModifierConfig);

    StatModifier Create(string statKey, OperatorType operatorType, float value, float duration);
}

public class StatModifierFactory : IStatModifierFactory
{
    public StatModifier Create(StatDefinition statDefinition, OperatorType operatorType, float value, float duration)
    {
        IOperationStrategy strategy = operatorType switch
        {
            OperatorType.Add => new AddOperation(value),
            OperatorType.Multiply => new MultiplyOperation(value),
            OperatorType.Percentage => new PercentageOperation(value),
            _ => throw new ArgumentOutOfRangeException(nameof(operatorType), operatorType, null),
        };

        return new StatModifier(statDefinition, strategy, duration);
    }

    public StatModifier Create(StatModifierConfig statModifierConfig)
    {
        return Create(statModifierConfig.StatDefinition, statModifierConfig.OperatorType, statModifierConfig.Value, statModifierConfig.Duration);
    }

    public StatModifier Create(string statKey, OperatorType operatorType, float value, float duration)
    {
        StatDefinition statDefinition = StatRegistryProvider.Instance.Registry.Get(statKey);

        IOperationStrategy strategy = operatorType switch
        {
            OperatorType.Add => new AddOperation(value),
            OperatorType.Multiply => new MultiplyOperation(value),
            OperatorType.Percentage => new PercentageOperation(value),
            _ => throw new ArgumentOutOfRangeException(nameof(operatorType), operatorType, null),
        };

        return new StatModifier(statDefinition, strategy, duration);
    }
}