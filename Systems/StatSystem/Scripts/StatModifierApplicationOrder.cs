using System.Collections.Generic;

public interface IStatModifierApplicationOrder
{
    float Apply(IReadOnlyList<StatModifier> statModifiers, float baseValue);
}

public class NormalStatModifierApplicationOrder : IStatModifierApplicationOrder
{
    public float Apply(IReadOnlyList<StatModifier> statModifiers, float baseValue)
    {
        int count = statModifiers.Count;

        // First pass: Add operations
        for (int i = 0; i < count; i++)
        {
            var modifier = statModifiers[i];
            if (modifier.Strategy is AddOperation)
            {
                baseValue = modifier.Strategy.Calculate(baseValue);
            }
        }

        // Second pass: Multiply operations
        for (int i = 0; i < count; i++)
        {
            var modifier = statModifiers[i];
            if (modifier.Strategy is MultiplyOperation)
            {
                baseValue = modifier.Strategy.Calculate(baseValue);
            }
        }

        // Third pass: Percentage operations
        for (int i = 0; i < count; i++)
        {
            var modifier = statModifiers[i];
            if (modifier.Strategy is PercentageOperation)
            {
                baseValue = modifier.Strategy.Calculate(baseValue);
            }
        }

        return baseValue;
    }
}