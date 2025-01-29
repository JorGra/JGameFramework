using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using JG.Tools;

public class URPVolumeVignetteEffector : MonoBehaviour
{
    private Volume volume;
    private Vignette vignetteOverride;

    // Store "baseline" (the value already set in the Volume asset)
    private float baselineIntensity;

    private IEventBinding<URPVignetteEvent> vignetteBinding;

    // Track active events in a list
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

        // If the volume already has a Vignette override, store its default intensity as our baseline
        if (volume.profile.TryGet(out vignetteOverride))
        {
            baselineIntensity = vignetteOverride.intensity.value;
        }
    }

    private void OnEnable()
    {
        vignetteBinding = new EventBinding<URPVignetteEvent>(OnVignetteEvent);
        EventBus<URPVignetteEvent>.Register(vignetteBinding);
    }

    private void OnDisable()
    {
        EventBus<URPVignetteEvent>.Deregister(vignetteBinding);
    }

    private void OnVignetteEvent(URPVignetteEvent evt)
    {
        if (volume == null) return;

        // Ensure there's a Vignette override. If not, add one and capture its baseline
        if (!volume.profile.TryGet(out vignetteOverride))
        {
            vignetteOverride = volume.profile.Add<Vignette>(true);
            baselineIntensity = vignetteOverride.intensity.value;
        }

        // Add a new active effect
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

        // Start by setting totalIntensity to 0. We'll add all active effects
        float totalIntensity = 0f;

        for (int i = activeVignettes.Count - 1; i >= 0; i--)
        {
            var v = activeVignettes[i];
            float elapsed = Time.time - v.startTime;
            if (elapsed >= v.duration)
            {
                // This effect is finished
                activeVignettes.RemoveAt(i);
                continue;
            }

            float t = elapsed / v.duration;
            float curveValue = v.curve.Evaluate(t); // 0..1
            float currentIntensity = v.baseIntensity * curveValue;
            totalIntensity += currentIntensity;
        }

        // If no active effects, totalIntensity = 0; we revert to baseline
        // Otherwise, we add the sum on top of baseline
        float finalIntensity = baselineIntensity + totalIntensity;

        // Apply final values
        vignetteOverride.active = (finalIntensity > 0f) || (baselineIntensity > 0f);
        vignetteOverride.intensity.value = finalIntensity;
    }
}


// This carries all data needed for a time-based, curve-driven vignette effect.
public struct URPVignetteEvent : IEvent
{
    public float baseIntensity;       // The maximum intensity used in the curve
    public AnimationCurve intensityCurve; // Evaluated from 0..1 over the effect's duration
    public float duration;            // How long (in seconds) the vignette should last
    public Color color;               // We can attempt to blend colors, or ignore them
    public bool active;               // Whether to enable the vignette override
    public float smoothness;          // Additional parameters if you like

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
