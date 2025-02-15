using UnityEngine;

[CreateAssetMenu(fileName = "StatModifierConfig", menuName = "Gameplay/Stats/StatModifierConfig")]
public class StatModifierConfig : ScriptableObject
{
    public StatDefinition statDefinition;
    public OperatorType OperatorType;
    public float Value;
    public float Duration;
}