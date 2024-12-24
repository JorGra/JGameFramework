using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponController : MonoBehaviour
{
    GameObject currentWeapon;
    public Transform mountPoint;
    public GameObject targetMarkerPrefab;
    WeaponFactory weaponFactory = new WeaponFactory();

    IWeapon weapon;
    IWeaponState currentState;


    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            currentState.OnMouseDown(this, transform);
        }

        if(Input.GetMouseButtonUp(0))
        {
            currentState.OnMouseUp(this, transform);
        }

        currentState.OnUpdate(this, transform);
        
    }

    public void Use(Transform target, float windupPower = 1f)
    {
        if (weapon == null)
        {
            Debug.Log("No weapon equipped");
            return;
        }
        weapon.Use(target, windupPower);
    }

    public void EquipWeapon(WeaponConfig config)
    {
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }

        currentState = config.attackStrategy is AoEStrategy? new TargetedState(targetMarkerPrefab) : new NonTargetState();

        currentWeapon = weaponFactory.CreateWeapon(config);
        weapon = currentWeapon.GetComponent<IWeapon>();
        currentWeapon.transform.SetParent(mountPoint);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;
    }
}
