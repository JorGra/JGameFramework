using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using JG.Tools;

public class URPVolumeBloomEffector : MonoBehaviour
{
    private Volume volume;
    private Bloom bloomOverride;

    // Baseline from the Volume’s default Bloom intensity
    private float baselineIntensity;

    private IEventBinding<URPBloomEvent> bloomBinding;

    private readonly List<ActiveBloom> activeBlooms = new List<ActiveBloom>();

    private class ActiveBloom
    {
        public float startTime;
        public float duration;
        public float baseIntensity;
        public AnimationCurve curve;
        public float threshold;
        public float scatter;
        public Color tintColor;
        public bool active;
    }

    private void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            Debug.LogWarning($"{name}: No Volume found on this GameObject.");
            return;
        }

        // If we already have a Bloom override, capture the existing intensity as the baseline
        if (volume.profile.TryGet(out bloomOverride))
        {
            baselineIntensity = bloomOverride.intensity.value;
        }
    }

    private void OnEnable()
    {
        bloomBinding = new EventBinding<URPBloomEvent>(OnBloomEvent);
        EventBus<URPBloomEvent>.Register(bloomBinding);
    }

    private void OnDisable()
    {
        EventBus<URPBloomEvent>.Deregister(bloomBinding);
    }

    private void OnBloomEvent(URPBloomEvent evt)
    {
        if (volume == null) return;

        if (!volume.profile.TryGet(out bloomOverride))
        {
            bloomOverride = volume.profile.Add<Bloom>(true);
            baselineIntensity = bloomOverride.intensity.value;
        }

        // Add a new active bloom
        activeBlooms.Add(new ActiveBloom
        {
            startTime = Time.time,
            duration = evt.duration,
            baseIntensity = evt.baseIntensity,
            curve = evt.intensityCurve,
            threshold = evt.threshold,
            scatter = evt.scatter,
            tintColor = evt.tintColor,
            active = evt.active
        });
    }

    private void Update()
    {
        if (bloomOverride == null) return;

        float totalIntensity = 0f;

        for (int i = activeBlooms.Count - 1; i >= 0; i--)
        {
            var b = activeBlooms[i];
            float elapsed = Time.time - b.startTime;
            if (elapsed >= b.duration)
            {
                activeBlooms.RemoveAt(i);
                continue;
            }

            float t = elapsed / b.duration;
            float curveVal = b.curve.Evaluate(t);
            float currentIntensity = b.baseIntensity * curveVal;
            totalIntensity += currentIntensity;
        }

        float finalIntensity = baselineIntensity + totalIntensity;
        bloomOverride.active = (finalIntensity > 0f) || (baselineIntensity > 0f);
        bloomOverride.intensity.value = finalIntensity;
    }
}


/// <summary>
/// Describes a bloom effect that plays over a certain duration,
/// with an intensity curve and optional color/tint.
/// </summary>
public struct URPBloomEvent : IEvent
{
    // The "peak" intensity
    public float baseIntensity;

    // Duration in seconds
    public float duration;

    // A curve sampled from t=0 to t=1 to scale the intensity over time
    public AnimationCurve intensityCurve;

    // Additional Bloom parameters
    public float threshold;   // Bloom threshold
    public float scatter;     // Bloom scatter
    public Color tintColor;   // Bloom tint
    public bool active;       // Whether to enable the override

    public URPBloomEvent(
        float baseIntensity,
        float duration,
        AnimationCurve intensityCurve,
        float threshold,
        float scatter,
        Color tintColor,
        bool active
    )
    {
        this.baseIntensity = baseIntensity;
        this.duration = duration;
        this.intensityCurve = intensityCurve;
        this.threshold = threshold;
        this.scatter = scatter;
        this.tintColor = tintColor;
        this.active = active;
    }
}


