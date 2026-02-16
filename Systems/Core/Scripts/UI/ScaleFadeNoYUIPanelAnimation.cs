using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Panel Animations/Scale Fade No Y", fileName = "ScaleFadeNoYUIPanelAnimation")]
public class ScaleFadeNoYUIPanelAnimation : UIPanelAnimation
{
    [SerializeField] private float canvasSpeedMultiplier = 3f;
    [SerializeField] private float overshoot = 0f; // 0 keeps legacy behaviour

    public override IEnumerator PlayOpen(UIPanelAnimated panel, CanvasGroup canvasGroup, float duration, Vector3 initialScale)
    {
        Transform target = panel ? panel.transform : null;
        if (target == null) yield break;

        Vector3 startScale = new Vector3(0f, initialScale.y, initialScale.z);
        Vector3 endScale = initialScale;
        float elapsed = 0f;

        target.localScale = startScale;

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (elapsed < duration)
        {
            float tScale = elapsed / duration;
            float tFade = Mathf.Clamp01(tScale * canvasSpeedMultiplier);
            tScale = EaseOutBack(tScale, overshoot);

            // Only X changes; Y and Z stay at initial values.
            float x = Mathf.LerpUnclamped(startScale.x, endScale.x, tScale);
            target.localScale = new Vector3(x, endScale.y, endScale.z);

            if (canvasGroup) canvasGroup.alpha = tFade;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        target.localScale = endScale;

        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public override IEnumerator PlayClose(UIPanelAnimated panel, CanvasGroup canvasGroup, float duration, Vector3 initialScale)
    {
        Transform target = panel ? panel.transform : null;
        if (target == null) yield break;

        Vector3 startScale = target.localScale;
        Vector3 endScale = new Vector3(0f, initialScale.y, initialScale.z);
        float elapsed = 0f;

        if (canvasGroup)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (elapsed < duration)
        {
            float tScale = elapsed / duration;
            float tFade = Mathf.Clamp01(tScale * canvasSpeedMultiplier);

            float x = Mathf.Lerp(startScale.x, endScale.x, tScale);
            target.localScale = new Vector3(x, endScale.y, endScale.z);

            if (canvasGroup) canvasGroup.alpha = 1f - tFade;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        target.localScale = endScale;
        if (canvasGroup) canvasGroup.alpha = 0f;

        if (canvasGroup)
        {
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private static float EaseOutBack(float t, float strength)
    {
        if (strength <= 0f) return t;
        float s = 1.70158f + strength;
        t -= 1f;
        return (t * t * ((s + 1f) * t + s)) + 1f;
    }
}
