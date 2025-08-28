using System;

public enum OperatorType { Add, Multiply, Percentage }

public interface IStatModifierFactory
{
    StatModifier Create(IStatDefinition statDefinition, OperatorType operatorType, float value, float duration);
    StatModifier Create(StatModifierConfig statModifierConfig);
    StatModifier Create(string statKey, OperatorType operatorType, float value, float duration);
}

public class StatModifierFactory : IStatModifierFactory
{
    public StatModifier Create(IStatDefinition statDefinition, OperatorType operatorType, float value, float duration)
        => Create(statDefinition?.Key, operatorType, value, duration);

    public StatModifier Create(StatModifierConfig config)
        => Create(config.Key, config.OperatorType, config.Value, config.Duration);

    public StatModifier Create(string statKey, OperatorType operatorType, float value, float duration)
    {
        if (string.IsNullOrWhiteSpace(statKey))
            throw new ArgumentException("statKey cannot be null/empty", nameof(statKey));

        IOperationStrategy strategy = operatorType switch
        {
            OperatorType.Add => new AddOperation(value),
            OperatorType.Multiply => new MultiplyOperation(value),
            OperatorType.Percentage => new PercentageOperation(value),
            _ => throw new ArgumentOutOfRangeException(nameof(operatorType), operatorType, null),
        };

        return new StatModifier(statKey, strategy, duration);
    }
}
