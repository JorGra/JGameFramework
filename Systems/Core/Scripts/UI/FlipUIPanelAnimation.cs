using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Panel Animations/Flip", fileName = "FlipUIPanelAnimation")]
public class FlipUIPanelAnimation : UIPanelAnimation
{
    [SerializeField, Tooltip("Axis to flip around. Y gives a card-turn effect; X flips vertically.")] private Vector3 rotationAxis = new Vector3(0f, 1f, 0f);
    [SerializeField, Tooltip("Degrees the panel starts/ends at when flipped. 90 means edge-on; 120 exaggerates the turn.")] private float flipAngle = 90f;
    [SerializeField, Tooltip("Controls how quickly the fade happens relative to rotation.")] private float canvasSpeedMultiplier = 2f;

    public override IEnumerator PlayOpen(UIPanelAnimated panel, CanvasGroup canvasGroup, float duration, Vector3 initialScale)
    {
        if (panel == null) yield break;

        Transform target = panel.transform;
        Quaternion baseRot = panel.InitialRotation;
        Quaternion startRot = Quaternion.AngleAxis(flipAngle, rotationAxis) * baseRot;

        target.localRotation = startRot;
        target.localScale = initialScale;

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

            target.localRotation = Quaternion.SlerpUnclamped(startRot, baseRot, eased);
            if (canvasGroup) canvasGroup.alpha = tFade;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        target.localRotation = baseRot;

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

        Transform target = panel.transform;
        Quaternion baseRot = panel.InitialRotation;
        Quaternion endRot = Quaternion.AngleAxis(flipAngle, rotationAxis) * baseRot;

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

            target.localRotation = Quaternion.SlerpUnclamped(baseRot, endRot, eased);
            if (canvasGroup) canvasGroup.alpha = 1f - tFade;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        target.localRotation = endRot;
        if (canvasGroup) canvasGroup.alpha = 0f;
    }
}
