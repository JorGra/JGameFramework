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
