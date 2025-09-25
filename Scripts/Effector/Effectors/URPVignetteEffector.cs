using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPVolumeVignetteEffector : MonoBehaviour
{
    private Volume volume;
    private Vignette vignetteOverride;

    private float baselineIntensity;

    private readonly List<ActiveVignette> activeVignettes = new List<ActiveVignette>();

    private class ActiveVignette
    {
        public float startTime;
        public float duration;
        public float baseIntensity;
        public AnimationCurve curve;
        public Color color;
        public bool active;
        public float smoothness;
    }

    private void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            Debug.LogWarning($"{name}: No Volume found on this GameObject.");
            return;
        }

        if (volume.profile.TryGet(out vignetteOverride))
        {
            baselineIntensity = vignetteOverride.intensity.value;
        }
    }

    private void OnEnable()
    {
        this.SubscribeEvent<URPVignetteEvent>(OnVignetteEvent);
    }

    private void OnVignetteEvent(URPVignetteEvent evt)
    {
        if (volume == null) return;

        if (!volume.profile.TryGet(out vignetteOverride))
        {
            vignetteOverride = volume.profile.Add<Vignette>(true);
            baselineIntensity = vignetteOverride.intensity.value;
        }

        activeVignettes.Add(new ActiveVignette
        {
            startTime = Time.time,
            duration = evt.duration,
            baseIntensity = evt.baseIntensity,
            curve = evt.intensityCurve,
            color = evt.color,
            active = evt.active,
            smoothness = evt.smoothness,
        });
    }

    private void Update()
    {
        if (vignetteOverride == null) return;

        float totalIntensity = 0f;

        for (int i = activeVignettes.Count - 1; i >= 0; i--)
        {
            var v = activeVignettes[i];
            float elapsed = Time.time - v.startTime;
            if (elapsed >= v.duration)
            {
                activeVignettes.RemoveAt(i);
                continue;
            }

            float t = elapsed / v.duration;
            float curveValue = v.curve.Evaluate(t);
            float currentIntensity = v.baseIntensity * curveValue;
            totalIntensity += currentIntensity;
        }

        float finalIntensity = baselineIntensity + totalIntensity;

        vignetteOverride.active = (finalIntensity > 0f) || (baselineIntensity > 0f);
        vignetteOverride.intensity.value = finalIntensity;
    }
}

public struct URPVignetteEvent : IEvent
{
    public float baseIntensity;
    public AnimationCurve intensityCurve;
    public float duration;
    public Color color;
    public bool active;
    public float smoothness;

    public URPVignetteEvent(
        float baseIntensity,
        AnimationCurve intensityCurve,
        float duration,
        Color color,
        bool active,
        float smoothness = 0.5f
    )
    {
        this.baseIntensity = baseIntensity;
        this.intensityCurve = intensityCurve;
        this.duration = duration;
        this.color = color;
        this.active = active;
        this.smoothness = smoothness;
    }
}
