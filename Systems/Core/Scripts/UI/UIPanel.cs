using System;
using UnityEngine;

public class UIPanel : MonoBehaviour
{
    public bool IsOpen { get; protected set; } = false;
    public Action OnPanelOpened;
    public Action OnPanelClosed;

    public virtual void Open()
    {
        if (!IsOpen)
        {
            gameObject.SetActive(true);
            IsOpen = true;
            OnPanelOpened?.Invoke();
        }
    }

    public virtual void Close()
    {
        if (IsOpen)
        {
            gameObject.SetActive(false);
            IsOpen = false;
            OnPanelClosed?.Invoke();
        }
    }
    public virtual void OnDisable()
    {
        IsOpen = false;
    }
}
