using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace JG.Samples
{
    public class ExampleWeapon : WeaponBase
    {
        public override void Use(Transform target, float windUpPower = 1f)
        {
            Debug.Log("Using " + config.weaponName + " to deal " + config.weaponDamage + " damage");

            if (config.attackCommand != null)
            {
                config.attackCommand.Execute(transform.position, transform.rotation);
            }
        }
    }

}