using System.Collections;
using UnityEngine;

public class UIPanelAnimated : UIPanel
{
    [SerializeField, Tooltip("Seconds the open/close animation lasts. Applies to all animation profiles.")] private float animationDuration = 0.2f;
    [SerializeField, Tooltip("Optional ScriptableObject profile that defines how this panel animates. Leave empty to use the built-in Scale+Fade.")] private UIPanelAnimation animationProfile;
    [SerializeField, Tooltip("CanvasGroup on the visual child. Required for fading and input blocking.")] private CanvasGroup canvasGroup;

    private Vector3 initialScale;
    private Vector3 initialLocalPosition;
    private Vector2 initialAnchoredPosition;
    private Quaternion initialRotation;
    private RectTransform rectTransform;

    private Coroutine openRoutine;
    private Coroutine closeRoutine;
    private bool isAnimatingClose;

    private static UIPanelAnimation defaultAnimationInstance;

    #region Accessors
    public Vector3 InitialLocalPosition => initialLocalPosition;
    public Vector2 InitialAnchoredPosition => initialAnchoredPosition;
    public Quaternion InitialRotation => initialRotation;
    public RectTransform RectT => rectTransform;
    public bool HasRectTransform => rectTransform != null;

    private UIPanelAnimation ActiveAnimation
    {
        get
        {
            if (animationProfile != null)
            {
                return animationProfile;
            }

            // Runtime fallback so existing panels keep their current behaviour without needing assets.
            if (defaultAnimationInstance == null)
            {
                defaultAnimationInstance = ScriptableObject.CreateInstance<ScaleFadeUIPanelAnimation>();
            }

            return defaultAnimationInstance;
        }
    }
    #endregion

    #region Life-cycle
    protected virtual void Awake()
    {
        rectTransform = transform as RectTransform;
        initialScale = transform.localScale;
        initialLocalPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        if (rectTransform)
        {
            initialAnchoredPosition = rectTransform.anchoredPosition;
        }

        if(canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>();

        ActiveAnimation.ApplyInitialState(this, canvasGroup, initialScale);

        IsOpen = false;
        if (canvasGroup)
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
        ActiveAnimation.SnapClosed(this, canvasGroup, initialScale);
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
        yield return ActiveAnimation.PlayOpen(this, canvasGroup, animationDuration, initialScale);
        IsOpen = true;
        openRoutine = null;
    }


    private IEnumerator AnimateClose()
    {
        yield return ActiveAnimation.PlayClose(this, canvasGroup, animationDuration, initialScale);

        if (canvasGroup)
            canvasGroup.gameObject.SetActive(false);

        IsOpen = false;
        isAnimatingClose = false;
        closeRoutine = null;
    }

    #endregion
}
