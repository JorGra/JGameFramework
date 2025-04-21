using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPanel : MonoBehaviour
{
    public bool IsOpen { get; protected set; } = true;
    public virtual void Open()
    {
        if (!IsOpen)
        {
            gameObject.SetActive(true);
            IsOpen = true;
        }
    }

    public virtual void Close()
    {
        if (IsOpen)
        {
            gameObject.SetActive(false);
            IsOpen = false;
        }
    }
}
