using System.Collections;
using UnityEngine;

public class UIPanelAnimated : UIPanel
{
    [SerializeField] private float animationDuration = 0.2f;   // editor-exposed

    private readonly float canvasSpeedMultiplier = 3f;

    private Vector3 initialScale;
    [SerializeField] private CanvasGroup canvasGroup;
    private Coroutine openRoutine;
    private Coroutine closeRoutine;
    private bool isAnimatingClose;

    #region Life-cycle
    protected virtual void Awake()
    {
        initialScale = transform.localScale;
        if(canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>();

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        IsOpen = false;
        canvasGroup.gameObject.SetActive(false);
    }
    #endregion

    #region Public API
    public override void Open()
    {
        if (IsOpen && !isAnimatingClose)
        {
            return;
        }

        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        isAnimatingClose = false;
        IsOpen = true;

        if (!canvasGroup.gameObject.activeSelf)
        {
            canvasGroup.gameObject.SetActive(true);
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
        }

        OnPanelOpened?.Invoke();
        openRoutine = StartCoroutine(AnimateOpen());
    }

    public void CloseImmediate()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }
        transform.localScale = Vector3.zero;
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.gameObject.SetActive(false);
        }
        isAnimatingClose = false;
        IsOpen = false;
        OnPanelClosed?.Invoke();
    }

    public override void Close()
    {
        if (!IsOpen && !isAnimatingClose)
        {
            return;
        }

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        if (!canvasGroup.gameObject.activeInHierarchy)
        {
            isAnimatingClose = false;
            IsOpen = false;
            return;
        }

        isAnimatingClose = true;
        IsOpen = false;

        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
        }

        OnPanelClosed?.Invoke();
        closeRoutine = StartCoroutine(AnimateClose());
    }
    #endregion

    #region Coroutines
    private IEnumerator AnimateOpen()
    {
        transform.localScale = Vector3.zero;
        float elapsed = 0f;

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (elapsed < animationDuration)
        {
            float tScale = elapsed / animationDuration;                     // 0 → 1
            float tFade = Mathf.Clamp01(tScale * canvasSpeedMultiplier);   // faster/slower

            transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, tScale);

            if (canvasGroup) canvasGroup.alpha = tFade;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = initialScale;

        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        IsOpen = true;
        openRoutine = null;
    }

    private IEnumerator AnimateClose()
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        if (canvasGroup)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (elapsed < animationDuration)
        {
            float tScale = elapsed / animationDuration;                     // 0 → 1
            float tFade = Mathf.Clamp01(tScale * canvasSpeedMultiplier);   // faster/slower

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, tScale);

            if (canvasGroup) canvasGroup.alpha = 1f - tFade;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.zero;
        if (canvasGroup) canvasGroup.alpha = 0f;

        canvasGroup.gameObject.SetActive(false);
        IsOpen = false;
        isAnimatingClose = false;
        closeRoutine = null;
    }
    #endregion
}
