using System.Collections;
using UnityEngine;

public class UIPanelAnimated : UIPanel
{
    [SerializeField]
    private float animationDuration = 0.2f; // How long the animation should take

    private Vector3 initialScale;

    protected virtual void Awake()
    {
        // Store the panel’s initial scale so we can scale back to it.
        initialScale = transform.localScale;
        IsOpen = false;
        gameObject.SetActive(false);
    }

    public override void Open()
    {
        if (IsOpen)
        {
            return;
        }

        // Activate the panel first (so it becomes visible).
        gameObject.SetActive(true);

        // Start the open animation coroutine.
        StartCoroutine(AnimateOpen());
    }

    public override void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        // Start the close animation coroutine.
        // The panel will deactivate at the end of the close animation.
        StartCoroutine(AnimateClose());
    }

    private IEnumerator AnimateOpen()
    {
        // Start at scale zero, then animate to the initial scale.
        transform.localScale = Vector3.zero;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final scale is set (in case of framerate variations).
        transform.localScale = initialScale;

        IsOpen = true;
    }

    private IEnumerator AnimateClose()
    {
        // Start from the current (likely initial) scale and animate to zero.
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final scale is zero, then deactivate the panel.
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        IsOpen = false;
    }
}
