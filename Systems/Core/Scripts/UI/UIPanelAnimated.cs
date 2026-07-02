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

        if (!gameObject.activeInHierarchy)
        {
            // We can't animate while our own GameObject (or an ancestor) is
            // inactive — coroutines don't tick. This happens when a tab switcher
            // has SetActive(false) on this content root. Snap straight to the
            // open visual state so the panel shows correctly once the ancestor
            // is re-activated, instead of throwing a "couldn't start coroutine"
            // error and leaving the panel logically closed.
            ActiveAnimation.SnapOpen(this, canvasGroup, initialScale);
            IsOpen = true;
            OnPanelOpened?.Invoke();
            return;
        }

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
        // If we're marked not-open, only short-circuit when the visual actually is closed.
        // Otherwise our logical state and canvasGroup state are out of sync — which happens
        // when the base UIPanel.OnDisable clobbers IsOpen=false after an ancestor is
        // SetActive(false) (e.g., a tab switcher hiding this panel's content root) without
        // touching our canvasGroup. In that case, run the close flow to resync.
        bool visuallyOpen = canvasGroup != null && canvasGroup.gameObject.activeSelf;
        if (!IsOpen && !isAnimatingClose && !visuallyOpen)
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
            // We can't animate while an ancestor is inactive (coroutines don't tick),
            // but we must still leave the panel in a fully-closed state — both logical
            // AND visual — otherwise re-activating the ancestor later reveals an
            // orphaned "open-looking" panel that no future Close() will hide (because
            // IsOpen is already false). Snap straight to the closed visual state.
            ActiveAnimation.SnapClosed(this, canvasGroup, initialScale);
            canvasGroup.gameObject.SetActive(false);
            isAnimatingClose = false;
            IsOpen = false;
            OnPanelClosed?.Invoke();
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
