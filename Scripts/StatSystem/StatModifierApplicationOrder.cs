using System.Collections.Generic;
using System.Linq;

public interface IStatModifierApplicationOrder
{
    float Apply(IEnumerable<StatModifier> statModifiers, float baseValue);
}


public class NormalStatModifierApplicationOrder : IStatModifierApplicationOrder
{
    public float Apply(IEnumerable<StatModifier> statModifiers, float baseValue)
    {
        var allModifiers = statModifiers.ToList();

        foreach (var modifier in allModifiers.Where(x => x.Strategy is AddOperation))
        {
            baseValue = modifier.Strategy.Calculate(baseValue);
        }

        foreach (var modifier in allModifiers.Where(x => x.Strategy is MultiplyOperation))
        {
            baseValue = modifier.Strategy.Calculate(baseValue);
        }

        foreach (var modifier in allModifiers.Where(x => x.Strategy is PercentageOperation))
        {
            baseValue = modifier.Strategy.Calculate(baseValue);
        }

        return baseValue;
    }
}