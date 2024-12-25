using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeaponFactory
{
    public GameObject CreateWeapon(WeaponConfig config)
    {
        if(config == null)
        {
            Debug.LogError("Weapon config is null");
            return null;
        }

        if (config.weaponPrefab == null)
        {
            Debug.LogError("Weapon prefab is null");
            return null;
        }




        GameObject weaponInstance = Object.Instantiate(config.weaponPrefab);

        var configurableWeapon = weaponInstance.GetComponent(typeof(IWeapon)) as IWeapon;

        if (configurableWeapon == null)
        {
            Debug.LogError("Weapon prefab does not have a IWeapon component");
            return null;
        }

        configurableWeapon.Initialize(config);
        return weaponInstance;
    }
}
