using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponFactory
{
    public GameObject CreateWeapon(WeaponConfig config)
    {
        if (config.weaponPrefab == null)
        {
            Debug.LogError("Weapon prefab is null");
            return null;
        }


        GameObject weaponInstance = Object.Instantiate(config.weaponPrefab);

        var configurableWeapon = weaponInstance.GetComponent(typeof(IWeapon)) as IWeapon;
        configurableWeapon.Initialize(config);
        return weaponInstance;
    }
}
