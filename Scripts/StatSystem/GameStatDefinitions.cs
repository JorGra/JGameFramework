/// <summary>
/// Provides strongly‑typed access to global stat definitions.
/// The IDs used here must match those assigned in the StatRegistry asset.
/// </summary>
public static class GameStatDefinitions
{
    public static StatDefinition MaxHealth => StatRegistryProvider.Instance.GetStatDefinitionById(0);
    public static StatDefinition Damage => StatRegistryProvider.Instance.GetStatDefinitionById(1);
    public static StatDefinition AttackSpeed => StatRegistryProvider.Instance.GetStatDefinitionById(2);
    public static StatDefinition MoveSpeed => StatRegistryProvider.Instance.GetStatDefinitionById(3);
    public static StatDefinition Range => StatRegistryProvider.Instance.GetStatDefinitionById(4);
    public static StatDefinition FireRate => StatRegistryProvider.Instance.GetStatDefinitionById(5);
    public static StatDefinition DamageToCastle => StatRegistryProvider.Instance.GetStatDefinitionById(6);
    public static StatDefinition RangedAttackRange => StatRegistryProvider.Instance.GetStatDefinitionById(7);
    public static StatDefinition MeleeAttackRange => StatRegistryProvider.Instance.GetStatDefinitionById(8);
    // Add additional global stat definitions here as needed.
}