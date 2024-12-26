using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged
}

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Gameplay/WeaponSystem/WeaponConfig", order = 1)]
public class WeaponConfig : ScriptableObject
{
    public string weaponName;
    public float weaponDamage;
    public GameObject weaponPrefab;
    public WeaponType weaponType;
    public IAttackCommand attackCommand;
    public ProjectileSettings projectileSettings;
}
