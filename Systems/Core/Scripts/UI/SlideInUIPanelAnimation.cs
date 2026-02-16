using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Panel Animations/Slide In", fileName = "SlideInUIPanelAnimation")]
public class SlideInUIPanelAnimation : UIPanelAnimation
{
    [SerializeField, Tooltip("Offset (in anchored UI units) from which the panel starts its slide on open. Positive X = from right; positive Y = from top.")] private Vector2 startOffset = new Vector2(0f, -200f);
    [SerializeField, Tooltip("Controls how quickly the fade happens relative to movement. >1 fades faster; <1 slower.")] private float canvasSpeedMultiplier = 2f;

    public override IEnumerator PlayOpen(UIPanelAnimated panel, CanvasGroup canvasGroup, float duration, Vector3 initialScale)
    {
        if (panel == null) yield break;

        RectTransform rect = panel.HasRectTransform ? panel.RectT : null;
        Vector2 initialPos = rect ? panel.InitialAnchoredPosition : (Vector2)panel.InitialLocalPosition;
        Vector2 startPos = initialPos + startOffset;

        if (rect)
            rect.anchoredPosition = startPos;
        else
            panel.transform.localPosition = startPos;

        panel.transform.localRotation = panel.InitialRotation;
        panel.transform.localScale = initialScale;

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float tFade = Mathf.Clamp01(t * canvasSpeedMultiplier);

            Vector2 pos = Vector2.LerpUnclamped(startPos, initialPos, eased);
            if (rect)
                rect.anchoredPosition = pos;
            else
                panel.transform.localPosition = pos;

            if (canvasGroup) canvasGroup.alpha = tFade;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (rect)
            rect.anchoredPosition = initialPos;
        else
            panel.transform.localPosition = initialPos;

        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public override IEnumerator PlayClose(UIPanelAnimated panel, CanvasGroup canvasGroup, float duration, Vector3 initialScale)
    {
        if (panel == null) yield break;

        RectTransform rect = panel.HasRectTransform ? panel.RectT : null;
        Vector2 initialPos = rect ? panel.InitialAnchoredPosition : (Vector2)panel.InitialLocalPosition;
        Vector2 endPos = initialPos + startOffset;

        if (canvasGroup)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float tFade = Mathf.Clamp01(t * canvasSpeedMultiplier);

            Vector2 pos = Vector2.LerpUnclamped(initialPos, endPos, eased);
            if (rect)
                rect.anchoredPosition = pos;
            else
                panel.transform.localPosition = pos;

            if (canvasGroup) canvasGroup.alpha = 1f - tFade;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (rect)
            rect.anchoredPosition = endPos;
        else
            panel.transform.localPosition = endPos;

        if (canvasGroup)
            canvasGroup.alpha = 0f;
    }
}
