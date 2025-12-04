using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IWeaponController
{
    IWeapon Weapon { get; set; }
    IWeaponState CurrentState { get; set; }

    void Use(Transform target, float windupPower = 1f);
    void EquipWeapon(WeaponConfig config);
}
