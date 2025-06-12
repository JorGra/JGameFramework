using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// UI-based full-screen fade (Overlay Image).
/// Listens for <see cref="FadeRequestEvent"/> via EventBus; no events are raised.
/// </summary>
[RequireComponent(typeof(Image))]
public class ScreenFadeController : MonoBehaviour
{
    /* ────────────────────────────────────────────
     *  Inspector fields
     * ────────────────────────────────────────── */
    [Header("Fade Defaults")]
    [SerializeField] private float defaultFadeDuration = 1f;
    [SerializeField] private Color defaultFadeColor = Color.black;

    /* ────────────────────────────────────────────
     *  Runtime
     * ────────────────────────────────────────── */
    private Image image;
    private Coroutine fadeCoroutine;
    private bool isFading;

    /// <summary>True while a fade coroutine is executing.</summary>
    public bool IsFading => isFading;

    /* Event-bus binding */
    private EventBinding<FadeRequestEvent> fadeRequestBinding;

    /* ────────────────────────────────────────────
     *  Unity lifecycle
     * ────────────────────────────────────────── */
    private void Awake()
    {
        image = GetComponent<Image>();
        SetFadeAmount(1f);               // Start fully opaque
        SetFadeColor(defaultFadeColor);
    }

    private void OnEnable()
    {
        fadeRequestBinding = new EventBinding<FadeRequestEvent>(OnFadeRequestReceived);
        EventBus<FadeRequestEvent>.Register(fadeRequestBinding);
    }

    private void OnDisable()
    {
        EventBus<FadeRequestEvent>.Deregister(fadeRequestBinding);
    }

    /* ────────────────────────────────────────────
     *  Event-bus callback
     * ────────────────────────────────────────── */
    private void OnFadeRequestReceived(FadeRequestEvent e)
    {
        if (isFading && !e.ForceReset) return;            // Ignore if busy
        if (isFading && e.ForceReset && fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);                 // Cancel current fade
        }

        if (e.FadeIn)
            FadeIn(e.Duration, e.ColorOverride, null, e.ForceReset);
        else
            FadeOut(e.Duration, e.ColorOverride, null, e.ForceReset);
    }

    /* ────────────────────────────────────────────
     *  Public API (optional direct use)
     * ────────────────────────────────────────── */
    /// <summary>Fade from clear ➜ opaque.</summary>
    public void FadeIn(float duration = -1f, Color? color = null,
                       UnityAction onComplete = null, bool forceReset = false)
    {
        StartFade(0f, 1f, duration, color, onComplete, forceReset);
    }

    /// <summary>Fade from opaque ➜ clear.</summary>
    public void FadeOut(float duration = -1f, Color? color = null,
                        UnityAction onComplete = null, bool forceReset = false)
    {
        StartFade(1f, 0f, duration, color, onComplete, forceReset);
    }

    /* ────────────────────────────────────────────
     *  Core fade logic
     * ────────────────────────────────────────── */
    private void StartFade(float startValue, float endValue,
                           float duration, Color? color,
                           UnityAction onComplete, bool forceReset)
    {
        if (isFading && !forceReset) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (duration < 0f) duration = defaultFadeDuration;
        if (color.HasValue) SetFadeColor(color.Value);

        fadeCoroutine = StartCoroutine(FadeCoroutine(startValue, endValue, duration, onComplete));
    }

    private IEnumerator FadeCoroutine(float startValue, float endValue,
                                      float duration, UnityAction onComplete)
    {
        isFading = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFadeAmount(Mathf.Lerp(startValue, endValue, t));
            yield return null;
        }

        SetFadeAmount(endValue);
        isFading = false;
        onComplete?.Invoke();
    }

    /* ────────────────────────────────────────────
     *  Helpers
     * ────────────────────────────────────────── */
    /// <summary>Instantly override fade colour (keeps current alpha).</summary>
    public void SetFadeColor(Color color)
    {
        image.color = new Color(color.r, color.g, color.b, image.color.a);
    }

    /// <summary>Set alpha (0–1) while preserving RGB.</summary>
    public void SetFadeAmount(float amount)
    {
        Color c = image.color;
        c.a = Mathf.Clamp01(amount);
        image.color = c;
    }
}
