using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Panel Animations/Scale Fade", fileName = "ScaleFadeUIPanelAnimation")]
public class ScaleFadeUIPanelAnimation : UIPanelAnimation
{
    [SerializeField] private float canvasSpeedMultiplier = 3f;
    [SerializeField] private float overshoot = 0f; // 0 keeps legacy behaviour

    public override IEnumerator PlayOpen(UIPanelAnimated panel, CanvasGroup canvasGroup, float duration, Vector3 initialScale)
    {
        Transform target = panel ? panel.transform : null;
        if (target == null) yield break;

        target.localScale = Vector3.zero;
        float elapsed = 0f;

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (elapsed < duration)
        {
            float tScale = elapsed / duration;                     // 0 -> 1
            float tFade = Mathf.Clamp01(tScale * canvasSpeedMultiplier);   // faster/slower
            tScale = EaseOutBack(tScale, overshoot);

            target.localScale = Vector3.LerpUnclamped(Vector3.zero, initialScale, tScale);

            if (canvasGroup) canvasGroup.alpha = tFade;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        target.localScale = initialScale;

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
        float elapsed = 0f;

        if (canvasGroup)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (elapsed < duration)
        {
            float tScale = elapsed / duration;                     // 0 -> 1
            float tFade = Mathf.Clamp01(tScale * canvasSpeedMultiplier);   // faster/slower

            target.localScale = Vector3.Lerp(startScale, Vector3.zero, tScale);

            if (canvasGroup) canvasGroup.alpha = 1f - tFade;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        target.localScale = Vector3.zero;
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
