using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingIndicator : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] private GameObject container;
    [SerializeField] private RectTransform loadingFill;   // Scales on X for progress
    [SerializeField] private GameObject loadingImage;  // Spins forever when active
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    // --------------------------------------------------------------------
    // Internal state
    // --------------------------------------------------------------------
    const float BACK_OVERSHOOT = 1.70158f;      // Same constant DOTween uses
    Coroutine fillRoutine, fadeRoutine, scaleRoutine, spinRoutine;

    void Start() => container.SetActive(false);

    /// <summary>Animate the progress bar and optionally change the label.</summary>
    public void SetLoadingIndicator(float value, string newText = null)
    {
        value = Mathf.Clamp01(value);

        if (fillRoutine != null) StopCoroutine(fillRoutine);
        fillRoutine = StartCoroutine(ScaleX(loadingFill, value, 0.5f));   // 0 → 1 in 0.5 s

        if (!string.IsNullOrEmpty(newText))
            text.text = newText;
    }

    /// <summary>Show or hide the whole loading overlay with the same feel as the tween version.</summary>
    public void ToggleLoadingScreen(bool enable)
    {
        if (enable)
        {
            container.SetActive(true);

            // reset starting states --------------------------------------
            loadingFill.localScale = new Vector3(0, 1, 1);
            container.transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;

            // begin animations ------------------------------------------
            fadeRoutine = StartCoroutine(Fade(canvasGroup, 0f, 1f, 1.5f));
            scaleRoutine = StartCoroutine(Scale(container.transform, Vector3.zero, Vector3.one, 0.5f, EaseOutBack));
            spinRoutine = StartCoroutine(SpinForever(loadingImage.transform, -360f, 1f));
        }
        else
        {
            // stop any old routines so they don’t fight the hide animation
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);

            fadeRoutine = StartCoroutine(Fade(canvasGroup, canvasGroup.alpha, 0f, 0.5f,
                                   () => container.SetActive(false)));
            scaleRoutine = StartCoroutine(Scale(container.transform, container.transform.localScale,
                                   Vector3.zero, 0.5f, EaseInBack));

            if (spinRoutine != null)
            {
                StopCoroutine(spinRoutine);
                spinRoutine = null;
            }
        }
    }

    // ====================================================================
    // -------------  Coroutines & helpers (generic, reusable) ------------
    // ====================================================================

    IEnumerator ScaleX(RectTransform target, float toX, float duration)
    {
        Vector3 from = target.localScale;
        Vector3 to = new Vector3(toX, from.y, from.z);

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            target.localScale = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        target.localScale = to;     // guarantee final value
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration, System.Action onDone = null)
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            cg.alpha = Mathf.LerpUnclamped(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
        onDone?.Invoke();
    }

    IEnumerator Scale(Transform tr, Vector3 from, Vector3 to, float duration, System.Func<float, float> ease)
    {
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float k = ease(t / duration);          // custom easing curve
            tr.localScale = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        tr.localScale = to;
    }

    IEnumerator SpinForever(Transform tr, float degreesPerCycle, float secondsPerCycle)
    {
        float degPerSecond = degreesPerCycle / secondsPerCycle;

        while (true)
        {
            tr.Rotate(0, 0, degPerSecond * Time.deltaTime, Space.Self);
            yield return null;
        }
    }

    // --------------------------------------------------------------------
    // Same “Back” eases that DOTween offers
    // --------------------------------------------------------------------
    static float EaseOutBack(float t)
    {
        float s = BACK_OVERSHOOT;
        t -= 1f;
        return t * t * ((s + 1f) * t + s) + 1f;
    }

    static float EaseInBack(float t)
    {
        float s = BACK_OVERSHOOT;
        return t * t * ((s + 1f) * t - s);
    }
}
