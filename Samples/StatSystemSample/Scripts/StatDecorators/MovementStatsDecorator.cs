using UnityEngine;


[CreateAssetMenu(fileName = "MovementStatsDecorator", menuName = "Gameplay/Stats/MovementDecorator")]
public class MovementStatsDecorator : StatsDecorator
{
    public float MoveSpeed = 3f;
    public override float? GetStatValue(StatType statType)
    {

        return statType switch
        {
            StatType.MoveSpeed => MoveSpeed,
            _ => null,
        };
    }
}