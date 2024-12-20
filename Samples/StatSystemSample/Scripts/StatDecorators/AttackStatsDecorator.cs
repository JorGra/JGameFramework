using UnityEngine;

[CreateAssetMenu(fileName = "AttackStatsDecorator", menuName = "Gameplay/Stats/AttackDecorator")]
public class AttackStatsDecorator : StatsDecorator
{
    public float Damage = 5f;
    public float AttackSpeed = 1f;
    public float Range = 10f;
    public override float? GetStatValue(StatType statType)
    {
        return statType switch
        {
            StatType.AttackSpeed => AttackSpeed,
            StatType.Damage => Damage,
            StatType.Range => Range,
            _ => null,
        };
    }
}
