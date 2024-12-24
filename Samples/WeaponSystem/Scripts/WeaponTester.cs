using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Samples {

    public class WeaponTester : MonoBehaviour
    {
        public WeaponController weaponController;
        public WeaponConfig weaponConfig;

        void Start()
        {
            weaponController.EquipWeapon(weaponConfig);
        }
    }
}