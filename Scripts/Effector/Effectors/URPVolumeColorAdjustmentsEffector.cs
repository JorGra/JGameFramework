using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPVolumeColorAdjustmentsEffector : MonoBehaviour
{
    private Volume volume;
    private ColorAdjustments colorAdjustments;

    // Baseline values from the Volume
    private float baselinePostExposure;
    private float baselineContrast;
    private float baselineHueShift;
    private float baselineSaturation;

    private IEventBinding<URPColorAdjustmentsEvent> binding;

    // Active effects list
    private readonly List<ActiveColorAdjustments> activeEffects = new List<ActiveColorAdjustments>();

    private class ActiveColorAdjustments
    {
        public float startTime;
        public float duration;
        public AnimationCurve curve;
        public float postExposureDelta;
        public float contrastDelta;
        public float hueShiftDelta;
        public float saturationDelta;
    }

    private void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            Debug.LogWarning($"{name}: No Volume found on this GameObject.");
            return;
        }

        // If there's already a ColorAdjustments override, store its baseline
        if (volume.profile.TryGet(out colorAdjustments))
        {
            baselinePostExposure = colorAdjustments.postExposure.value;
            baselineContrast = colorAdjustments.contrast.value;
            baselineHueShift = colorAdjustments.hueShift.value;
            baselineSaturation = colorAdjustments.saturation.value;
        }
    }

    private void OnEnable()
    {
        binding = new EventBinding<URPColorAdjustmentsEvent>(OnColorAdjustmentsEvent);
        EventBus<URPColorAdjustmentsEvent>.Register(binding);
    }

    private void OnDisable()
    {
        EventBus<URPColorAdjustmentsEvent>.Deregister(binding);
    }

    private void OnColorAdjustmentsEvent(URPColorAdjustmentsEvent evt)
    {
        if (volume == null) return;

        // Ensure we have ColorAdjustments override in the profile
        if (!volume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = volume.profile.Add<ColorAdjustments>(true);
            // If new, also update baseline from the newly created override
            baselinePostExposure = colorAdjustments.postExposure.value;
            baselineContrast = colorAdjustments.contrast.value;
            baselineHueShift = colorAdjustments.hueShift.value;
            baselineSaturation = colorAdjustments.saturation.value;
        }

        // Add a new record
        activeEffects.Add(new ActiveColorAdjustments
        {
            startTime = Time.time,
            duration = evt.duration,
            curve = evt.curve,
            postExposureDelta = evt.postExposureDelta,
            contrastDelta = evt.contrastDelta,
            hueShiftDelta = evt.hueShiftDelta,
            saturationDelta = evt.saturationDelta
        });
    }

    private void Update()
    {
        if (colorAdjustments == null) return;

        // Start with 0 total deltas in each parameter
        float totalPostExposure = 0f;
        float totalContrast = 0f;
        float totalHueShift = 0f;
        float totalSaturation = 0f;

        // Remove expired, sum active
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var a = activeEffects[i];
            float elapsed = Time.time - a.startTime;
            if (elapsed >= a.duration)
            {
                activeEffects.RemoveAt(i);
                continue;
            }

            float t = elapsed / a.duration;
            float curveVal = a.curve.Evaluate(t); // 0..1

            // Add partial deltas
            totalPostExposure += a.postExposureDelta * curveVal;
            totalContrast += a.contrastDelta * curveVal;
            totalHueShift += a.hueShiftDelta * curveVal;
            totalSaturation += a.saturationDelta * curveVal;
        }

        // Final values are baseline + sum of active effect deltas
        colorAdjustments.active = true;
        colorAdjustments.postExposure.value = baselinePostExposure + totalPostExposure;
        colorAdjustments.contrast.value = baselineContrast + totalContrast;
        colorAdjustments.hueShift.value = baselineHueShift + totalHueShift;
        colorAdjustments.saturation.value = baselineSaturation + totalSaturation;
    }
}


/// <summary>
/// Carries the data needed to animate color adjustments over time.
/// We add "Delta" values for each parameter, meaning "how much to add 
/// on top of the baseline" for postExposure, contrast, hueShift, saturation.
/// </summary>
public struct URPColorAdjustmentsEvent : IEvent
{
    public float duration;          // How long the effect should last (seconds).
    public AnimationCurve curve;    // Evaluated [0..1] over the effect's duration.

    // Each of these is the maximum "delta" from the baseline.
    // We'll multiply by the curve's value each frame.
    public float postExposureDelta;
    public float contrastDelta;
    public float hueShiftDelta;
    public float saturationDelta;

    public URPColorAdjustmentsEvent(
        float duration,
        AnimationCurve curve,
        float postExposureDelta,
        float contrastDelta,
        float hueShiftDelta,
        float saturationDelta
    )
    {
        this.duration = duration;
        this.curve = curve;
        this.postExposureDelta = postExposureDelta;
        this.contrastDelta = contrastDelta;
        this.hueShiftDelta = hueShiftDelta;
        this.saturationDelta = saturationDelta;
    }
}
