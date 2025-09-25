using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPVolumeColorAdjustmentsEffector : MonoBehaviour
{
    private Volume volume;
    private ColorAdjustments colorAdjustments;

    private float baselinePostExposure;
    private float baselineContrast;
    private float baselineHueShift;
    private float baselineSaturation;

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
        this.SubscribeEvent<URPColorAdjustmentsEvent>(OnColorAdjustmentsEvent);
    }

    private void OnColorAdjustmentsEvent(URPColorAdjustmentsEvent evt)
    {
        if (volume == null) return;

        if (!volume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = volume.profile.Add<ColorAdjustments>(true);
            baselinePostExposure = colorAdjustments.postExposure.value;
            baselineContrast = colorAdjustments.contrast.value;
            baselineHueShift = colorAdjustments.hueShift.value;
            baselineSaturation = colorAdjustments.saturation.value;
        }

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

        float totalPostExposure = 0f;
        float totalContrast = 0f;
        float totalHueShift = 0f;
        float totalSaturation = 0f;

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
            float curveVal = a.curve.Evaluate(t);

            totalPostExposure += a.postExposureDelta * curveVal;
            totalContrast += a.contrastDelta * curveVal;
            totalHueShift += a.hueShiftDelta * curveVal;
            totalSaturation += a.saturationDelta * curveVal;
        }

        colorAdjustments.active = true;
        colorAdjustments.postExposure.value = baselinePostExposure + totalPostExposure;
        colorAdjustments.contrast.value = baselineContrast + totalContrast;
        colorAdjustments.hueShift.value = baselineHueShift + totalHueShift;
        colorAdjustments.saturation.value = baselineSaturation + totalSaturation;
    }
}

public struct URPColorAdjustmentsEvent : IEvent
{
    public float duration;
    public AnimationCurve curve;
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
