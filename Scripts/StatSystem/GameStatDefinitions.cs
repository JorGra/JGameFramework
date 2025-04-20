using UnityEngine;

/// <summary>
/// Provides strongly‑typed access to global stat definitions.
/// Now looks them up by string key instead of integer ID.
/// </summary>
public static class GameStatDefinitions
{
    public static StatDefinition MaxHealth => StatRegistryProvider.Instance.Get("MaxHealth");
    public static StatDefinition Damage => StatRegistryProvider.Instance.Get("Damage");
    public static StatDefinition AttackSpeed => StatRegistryProvider.Instance.Get("AttackSpeed");
    public static StatDefinition MoveSpeed => StatRegistryProvider.Instance.Get("MoveSpeed");
    public static StatDefinition Range => StatRegistryProvider.Instance.Get("Range");
    public static StatDefinition InwardsDrag => StatRegistryProvider.Instance.Get("InwardsDrag");
    public static StatDefinition FireRate => StatRegistryProvider.Instance.Get("FireRate");
    public static StatDefinition DamageToCastle => StatRegistryProvider.Instance.Get("DamageToCastle");
    public static StatDefinition RangedAttackRange => StatRegistryProvider.Instance.Get("RangedAttackRange");
    public static StatDefinition MeleeAttackRange => StatRegistryProvider.Instance.Get("MeleeAttackRange");
    // Add additional global stats here...
}
