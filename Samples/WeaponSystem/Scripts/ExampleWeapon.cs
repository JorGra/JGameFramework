using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleWeapon : WeaponBase
{
    public override void Use(Transform transform)
    {
        Debug.Log("Using " + config.weaponName + " to deal " + config.weaponDamage + " damage");

        if (config.attackStrategy)
        {
            config.attackStrategy.Execute(transform.position, new GameObject("EffectPrefab"));
        }
    }
}

