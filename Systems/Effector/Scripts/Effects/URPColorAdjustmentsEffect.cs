using System.Collections;
using UnityEngine;

public class URPColorAdjustmentsEffect : EffectBase
{
    [Header("Color Adjustments Settings")]
    [Tooltip("Length of the effect in seconds.")]
    [SerializeField] private float duration = 1f;

    [Tooltip("Curve to blend the effect from 0..1 over time.")]
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Delta Values (added on top of baseline)")]
    [SerializeField] private float postExposureDelta = 1f;
    [SerializeField] private float contrastDelta = 10f;
    [SerializeField] private float hueShiftDelta = 20f;
    [SerializeField] private float saturationDelta = -10f;

    protected override IEnumerator PlayEffectLogic()
    {
        // Construct and raise the event
        EventBus<URPColorAdjustmentsEvent>.Raise(new URPColorAdjustmentsEvent(
            duration,
            intensityCurve,
            postExposureDelta,
            contrastDelta,
            hueShiftDelta,
            saturationDelta
        ));

        yield return null; // The actual effect is handled by the effector
    }
}
