using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ScreenFadeController : MonoBehaviour
{
    [Header("Fade Settings")]
    public float defaultFadeDuration = 1f;
    public Color defaultFadeColor = Color.black;

    private Coroutine fadeCoroutine;

    Image image;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        SetFadeAmount(1);
    }

    public void FadeIn(float duration = -1, Color? color = null, UnityAction onComplete = null)
    {
        StartFade(0, 1, duration, color, onComplete);
    }

    public void FadeOut(float duration = -1, Color? color = null, UnityAction onComplete = null)
    {
        StartFade(1, 0, duration, color, onComplete);
    }

    public void StartFade(float startValue, float endValue, float duration = -1, Color? color = null, UnityAction onComplete = null)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        if (duration < 0) duration = defaultFadeDuration;
        if (color.HasValue) SetFadeColor(color.Value);

        fadeCoroutine = StartCoroutine(FadeCoroutine(startValue, endValue, duration, onComplete));
    }

    private IEnumerator FadeCoroutine(float startValue, float endValue, float duration, UnityAction onComplete)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float fadeAmount = Mathf.Lerp(startValue, endValue, t);
            SetFadeAmount(fadeAmount);
            yield return null;
        }

        SetFadeAmount(endValue);
        onComplete?.Invoke();
    }

    public void SetFadeColor(Color color)
    {
        image.color = defaultFadeColor;
    }

    public void SetFadeAmount(float amount)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, amount);
    }
}
