using System.Collections.Generic;

public interface IStatModifierApplicationOrder
{
    float Apply(IReadOnlyList<StatModifier> statModifiers, in Query query);
}

public class NormalStatModifierApplicationOrder : IStatModifierApplicationOrder
{
    public float Apply(IReadOnlyList<StatModifier> statModifiers, in Query query)
    {
        int count = statModifiers.Count;
        float value = query.Value;

        // First pass: Add operations
        for (int i = 0; i < count; i++)
        {
            var s = statModifiers[i].ResolveStrategy(query.Provider);
            if (s is AddOperation) value = s.Calculate(value);
        }

        // Second pass: Multiply operations
        for (int i = 0; i < count; i++)
        {
            var s = statModifiers[i].ResolveStrategy(query.Provider);
            if (s is MultiplyOperation) value = s.Calculate(value);
        }

        // Third pass: Percentage operations
        for (int i = 0; i < count; i++)
        {
            var s = statModifiers[i].ResolveStrategy(query.Provider);
            if (s is PercentageOperation) value = s.Calculate(value);
        }

        return value;
    }
}
