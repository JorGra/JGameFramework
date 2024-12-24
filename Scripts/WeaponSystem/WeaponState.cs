using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IWeaponState
{
    void OnMouseDown(WeaponController controller, Transform playerTransform);
    void OnMouseUp(WeaponController controller, Transform playerTransform);
    void OnUpdate(WeaponController controller, Transform playerTransform);
}

public class NonTargetState : IWeaponState
{
    public void OnMouseDown(WeaponController controller, Transform playerTransform)
    {
        controller.Use(playerTransform);
    }

    public void OnMouseUp(WeaponController controller, Transform playerTransform)
    {
        
    }

    public void OnUpdate(WeaponController controller, Transform playerTransform)
    {
    }
}

public class TargetedState : IWeaponState
{
    GameObject targetMarker;

    public TargetedState(GameObject targetMarker)
    {
        this.targetMarker = targetMarker;
        this.targetMarker.SetActive(false);
    }
    public void OnMouseDown(WeaponController controller, Transform playerTransform)
    {
        targetMarker.SetActive(true);
    }

    public void OnMouseUp(WeaponController controller, Transform playerTransform)
    {
        targetMarker.SetActive(false);
        controller.Use(playerTransform);
    }

    public void OnUpdate(WeaponController controller, Transform playerTransform)
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
    public void OnMouseDown(WeaponController controller, Transform playerTransform)
    {
        currentWindUpTime = 0f;

    }

    public void OnMouseUp(WeaponController controller, Transform playerTransform)
    {
        if (currentWindUpTime > MinWindUpTime)
        {
            var windUpPower = 0.5f + Mathf.Clamp01(currentWindUpTime / MaxWindUpTime);
            //TODO: Pass firepower to the controller
            controller.Use(playerTransform, windUpPower);
        }

    }

    public void OnUpdate(WeaponController controller, Transform playerTransform)
    {
        currentWindUpTime += Time.deltaTime;
    }
}
