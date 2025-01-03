using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IWeaponState
{
    void OnMouseDown(IWeaponController controller, Transform playerTransform);
    void OnMouseUp(IWeaponController controller, Transform playerTransform);
    void OnUpdate(IWeaponController controller, Transform playerTransform);
}

public class SingleFireState : IWeaponState
{
    public void OnMouseDown(IWeaponController controller, Transform playerTransform)
    {
        controller.Use(playerTransform);
    }

    public void OnMouseUp(IWeaponController controller, Transform playerTransform)
    {
        
    }

    public void OnUpdate(IWeaponController controller, Transform playerTransform)
    {
    }
}

public class AutoFireState : IWeaponState
{
    public void OnMouseDown(IWeaponController controller, Transform playerTransform)
    {

    }

    public void OnMouseUp(IWeaponController controller, Transform playerTransform)
    {

    }

    public void OnUpdate(IWeaponController controller, Transform playerTransform)
    {
        controller.Use(playerTransform);
    }
}
public class TargetSelectionState : IWeaponState
{
    GameObject targetMarker;

    public TargetSelectionState(GameObject targetMarker)
    {
        this.targetMarker = targetMarker;
        this.targetMarker.SetActive(false);
    }
    public void OnMouseDown(IWeaponController controller, Transform playerTransform)
    {
        targetMarker.SetActive(true);
    }

    public void OnMouseUp(IWeaponController controller, Transform playerTransform)
    {
        targetMarker.SetActive(false);
        controller.Use(playerTransform);
    }

    public void OnUpdate(IWeaponController controller, Transform playerTransform)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Ground")))
        {
            targetMarker.transform.position = hit.point;
            targetMarker.transform.rotation = Quaternion.LookRotation(Vector3.forward, hit.normal);
        }
    }

}
public class WindUpState : IWeaponState
{
    public float MaxWindUpTime = 3f;
    public float MinWindUpTime = 1f;

    float currentWindUpTime = 0f;
    public WindUpState(WeaponConfig weaponConfig)
    {
        //TODO: Use weaponConfig to set windup time
    }
    public void OnMouseDown(IWeaponController controller, Transform playerTransform)
    {
        currentWindUpTime = 0f;

    }

    public void OnMouseUp(IWeaponController controller, Transform playerTransform)
    {
        if (currentWindUpTime > MinWindUpTime)
        {
            var windUpPower = 0.5f + Mathf.Clamp01(currentWindUpTime / MaxWindUpTime);
            //TODO: Pass firepower to the controller
            controller.Use(playerTransform, windUpPower);
        }

    }

    public void OnUpdate(IWeaponController controller, Transform playerTransform)
    {
        currentWindUpTime += Time.deltaTime;
    }
}
