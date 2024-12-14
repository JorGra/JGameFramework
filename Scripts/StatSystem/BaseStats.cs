using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BaseStats", menuName = "Gameplay/Stats/BaseStats")]
public class BaseStats : ScriptableObject
{
    public int MaxHealth = 10;

}


[CreateAssetMenu(fileName = "PlayerStats", menuName = "Gameplay/Stats/PlayerStats")]
public class PlayerStats : BaseStats
{
    float DamagePerc = 100f;
}


[CreateAssetMenu(fileName = "ShipStats", menuName = "Gameplay/Stats/ShipStats")]
public class ShipStats : BaseStats
{

}