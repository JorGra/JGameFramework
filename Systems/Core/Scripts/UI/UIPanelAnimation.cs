using System.Collections;
using UnityEngine;

/// <summary>
/// Pluggable animation profile for UIPanelAnimated. Keeps CanvasGroup-driven visibility
/// while allowing different motion/easing implementations.
/// </summary>
public abstract class UIPanelAnimation : ScriptableObject
{
    /// <summary>
    /// Ensure the panel starts in a consistent closed state. Called from UIPanelAnimated.Awake.
    /// </summary>
    public virtual void ApplyInitialState(UIPanelAnimated panel, CanvasGroup canvasGroup, Vector3 initialScale)
    {
        if (panel != null)
        {
            if (panel.HasRectTransform)
                panel.RectT.anchoredPosition = panel.InitialAnchoredPosition;
            else
                panel.transform.localPosition = panel.InitialLocalPosition;

            panel.transform.localRotation = panel.InitialRotation;
        }

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    public abstract IEnumerator PlayOpen(UIPanelAnimated panel, CanvasGroup canvasGroup, float duration, Vector3 initialScale);

    public abstract IEnumerator PlayClose(UIPanelAnimated panel, CanvasGroup canvasGroup, float duration, Vector3 initialScale);

    /// <summary>
    /// Immediate open without animation. Used when the panel's own GameObject is
    /// inactive (e.g. a tab switcher has deactivated this content root), so a
    /// coroutine can't run — but the panel must still end up in a fully-open
    /// visual state for when the ancestor is re-activated.
    /// </summary>
    public virtual void SnapOpen(UIPanelAnimated panel, CanvasGroup canvasGroup, Vector3 initialScale)
    {
        if (panel)
        {
            if (panel.HasRectTransform)
                panel.RectT.anchoredPosition = panel.InitialAnchoredPosition;
            else
                panel.transform.localPosition = panel.InitialLocalPosition;

            panel.transform.localRotation = panel.InitialRotation;
            panel.transform.localScale = initialScale;
        }

        if (canvasGroup)
        {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// Immediate close without animation (used by CloseImmediate).
    /// </summary>
    public virtual void SnapClosed(UIPanelAnimated panel, CanvasGroup canvasGroup, Vector3 initialScale)
    {
        if (panel)
        {
            if (panel.HasRectTransform)
                panel.RectT.anchoredPosition = panel.InitialAnchoredPosition;
            else
                panel.transform.localPosition = panel.InitialLocalPosition;

            panel.transform.localRotation = panel.InitialRotation;
            panel.transform.localScale = Vector3.zero;
        }

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.gameObject.SetActive(false);
        }
    }
}
