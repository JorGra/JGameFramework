﻿using System;

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
        return Create(statModifierConfig.statDefinition, statModifierConfig.OperatorType, statModifierConfig.Value, statModifierConfig.Duration);
    }
}