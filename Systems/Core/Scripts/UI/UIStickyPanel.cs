using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStickyPanel : UIPanelAnimated
{
    RectTransform uiElement;
    [SerializeField] Transform target; // The 3D object or position we want to track, get this from somewhere
    Canvas canvas;
    Camera mainCam;

    protected virtual void Start()
    {
        uiElement = GetComponent<RectTransform>();
        mainCam = Camera.main;
        canvas = GetComponentInParent<Canvas>();
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        base.Close();
    }

    protected virtual void Update()
    {
        //UpdatePosition based on currently selected object
        //Close if clicked away

        if (target == null || uiElement == null || canvas == null)
            return;

        // 1) Convert the 3D world position to screen coordinates
        Vector3 screenPos = mainCam.WorldToScreenPoint(target.position);

        // 2) Convert screen coordinates to the Canvas’s local coordinates
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            null,//canvas.worldCamera, // pass null if using Screen Space - Overlay
            out Vector2 localPos
        );

        // 3) Assign that position to the UI element
        uiElement.anchoredPosition = localPos;

        //if (IsOpen)
        //{
        //    transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //}
    }

    public virtual void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
