using System.Collections;
using UnityEngine;

public class URPVignetteEffect : EffectBase
{
    [Header("URP Vignette Settings")]
    [SerializeField] private float baseIntensity = 0.4f;

    [Tooltip("Length of the effect in seconds")]
    [SerializeField] private float duration = 1f;

    [Tooltip("Curve that defines how intensity evolves over time (0..1 -> scale)")]
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [SerializeField] private Color vignetteColor = Color.black;
    [SerializeField] private bool vignetteEnabled = true;
    [SerializeField] private float smoothness = 0.5f;

    protected override IEnumerator PlayEffectLogic()
    {
        // Raise the event so the volume effector picks it up
        EventBus<URPVignetteEvent>.Raise(new URPVignetteEvent(
            baseIntensity,
            intensityCurve,
            duration,
            vignetteColor,
            vignetteEnabled,
            smoothness
        ));

        yield return null; // No local wait here; actual effect is in the volume effector
    }
}
