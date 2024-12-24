using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace JG.Samples { 
    public class Player : MonoBehaviour
    {
        public WeaponController weaponController;

        // Start is called before the first frame update
        void Start()
        {
            weaponController = GetComponent<WeaponController>();
        }

    }
}