using System;
using UnityEngine;


public interface IStatModifierFactory
{
    StatModifier Create(StatType statType, OperatorType operatorType, float value, float duration);
    StatModifier Create(StatModifierConfig statModifierConfig);
}

public class StatModifierFactory : IStatModifierFactory
{
    public StatModifier Create(StatType statType, OperatorType operatorType, float value, float duration)
    {
        return operatorType switch
        {
            OperatorType.Add => new BasicStatModifier(statType, x => x + value, duration),
            OperatorType.Multiply => new BasicStatModifier(statType, x => x * value, duration),
            _ => throw new ArgumentOutOfRangeException(nameof(operatorType), operatorType, null),
        };
    }

    public StatModifier Create(StatModifierConfig statModifierConfig)
    {
        return Create(statModifierConfig.StatType, statModifierConfig.OperatorType, statModifierConfig.Value, statModifierConfig.Duration);
    }
}

[CreateAssetMenu(fileName = "StatModifierConfig", menuName = "Gameplay/Stats/StatModifierConfig")]
public class StatModifierConfig : ScriptableObject
{
    public StatType StatType;
    public OperatorType OperatorType;
    public float Value;
    public float Duration;
}