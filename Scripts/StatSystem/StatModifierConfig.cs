using UnityEngine;

/// <summary>
/// Configuration for creating a <see cref="StatModifier"/> at runtime.
/// Stores a stat key and resolves the corresponding <see cref="StatDefinition"/> dynamically.
/// </summary>
[CreateAssetMenu(fileName = "StatModifierConfig", menuName = "Gameplay/Stats/StatModifierConfig")]
public class StatModifierConfig : ScriptableObject
{
    [Tooltip("Key of the stat to modify (must match your stats.json)")]
    [SerializeField] private string key;

    /// <summary>
    /// The stat key used to resolve the definition.
    /// </summary>
    public string Key => key;
    
    /// <summary>
    /// Resolved StatDefinition from the registry based on <see cref="Key"/>.
    /// </summary>
    public StatDefinition StatDefinition =>
        StatRegistryProvider.Instance.Registry.Get(Key);

    [Tooltip("Operator to apply to the stat.")]
    [SerializeField] private OperatorType operatorType;
    /// <summary>
    /// The type of operation for the modifier.
    /// </summary>
    public OperatorType OperatorType => operatorType;

    [Tooltip("Value used by the operator strategy.")]
    [SerializeField] private float value;
    /// <summary>
    /// Numeric value to apply (e.g. +10, ×1.5, +20%).
    /// </summary>
    public float Value => value;

    [Tooltip("Duration in seconds (0 = permanent).")]
    [SerializeField] private float duration;
    /// <summary>
    /// Lifetime of the modifier in seconds.
    /// </summary>
    public float Duration => duration;
}
