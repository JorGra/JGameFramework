using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using JG.Tools;

public class URPFilmGrainEffector : MonoBehaviour
{
    private Volume volume;
    private FilmGrain filmGrainOverride;

    // Baseline from the Volume's default FilmGrain settings
    private float baselineIntensity;
    private float baselineResponse;
    private FilmGrainLookup baselineType;

    private IEventBinding<URPFilmGrainEvent> grainBinding;

    private readonly List<ActiveFilmGrain> activeGrains = new List<ActiveFilmGrain>();

    private class ActiveFilmGrain
    {
        public float startTime;
        public float duration;
        public AnimationCurve curve;
        public float intensityDelta;
        public float responseDelta;
        public FilmGrainLookup? grainType;
    }

    private void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            Debug.LogWarning($"{name}: No Volume found on this GameObject.");
            return;
        }

        // If there's already a FilmGrain override, store its baseline
        if (volume.profile.TryGet(out filmGrainOverride))
        {
            baselineIntensity = filmGrainOverride.intensity.value;
            baselineResponse = filmGrainOverride.response.value;
            baselineType = filmGrainOverride.type.value;
        }
    }

    private void OnEnable()
    {
        grainBinding = new EventBinding<URPFilmGrainEvent>(OnFilmGrainEvent);
        EventBus<URPFilmGrainEvent>.Register(grainBinding);
    }

    private void OnDisable()
    {
        EventBus<URPFilmGrainEvent>.Deregister(grainBinding);
    }

    private void OnFilmGrainEvent(URPFilmGrainEvent evt)
    {
        if (volume == null) return;

        // Ensure we have a FilmGrain override
        if (!volume.profile.TryGet(out filmGrainOverride))
        {
            filmGrainOverride = volume.profile.Add<FilmGrain>(true);
            // Update baseline from newly created override if needed
            baselineIntensity = filmGrainOverride.intensity.value;
            baselineResponse = filmGrainOverride.response.value;
            baselineType = filmGrainOverride.type.value;
        }

        activeGrains.Add(new ActiveFilmGrain
        {
            startTime = Time.time,
            duration = evt.duration,
            curve = evt.curve,
            intensityDelta = evt.intensityDelta,
            responseDelta = evt.responseDelta,
            grainType = evt.grainType
        });
    }

    private void Update()
    {
        if (filmGrainOverride == null) return;

        float totalIntensityDelta = 0f;
        float totalResponseDelta = 0f;

        // For "type", we will pick the last event that had a type override
        // (If multiple events override type, whichever is newest remains.)
        FilmGrainLookup? lastTypeOverride = null;

        for (int i = activeGrains.Count - 1; i >= 0; i--)
        {
            var g = activeGrains[i];
            float elapsed = Time.time - g.startTime;
            if (elapsed >= g.duration)
            {
                // This event is finished
                activeGrains.RemoveAt(i);
                continue;
            }

            float t = elapsed / g.duration;
            float curveVal = g.curve.Evaluate(t);

            totalIntensityDelta += g.intensityDelta * curveVal;
            totalResponseDelta += g.responseDelta * curveVal;

            if (g.grainType.HasValue)
            {
                lastTypeOverride = g.grainType.Value;
            }
        }

        // Add the deltas to our baseline
        float finalIntensity = baselineIntensity + totalIntensityDelta;
        float finalResponse = baselineResponse + totalResponseDelta;

        // If finalIntensity is negative, you might clamp to 0..1 if you prefer:
        finalIntensity = Mathf.Clamp01(finalIntensity);
        finalResponse = Mathf.Max(0f, finalResponse); // or clamp if needed

        filmGrainOverride.active = true;
        filmGrainOverride.intensity.value = finalIntensity;
        filmGrainOverride.response.value = finalResponse;

        // If at least one effect has a grainType override, we use the last encountered
        if (lastTypeOverride.HasValue)
        {
            filmGrainOverride.type.value = lastTypeOverride.Value;
        }
        else
        {
            // Otherwise revert to baseline type
            filmGrainOverride.type.value = baselineType;
        }
    }
}

/// <summary>
/// Describes a time-based FilmGrain effect. 
/// We add an intensityDelta on top of a baseline. 
/// Also includes a 'responseDelta', in case you want to animate that too.
/// We can optionally choose a specific FilmGrainLookup 'type'.
/// </summary>
public struct URPFilmGrainEvent : IEvent
{
    public float duration;          // How long the effect lasts (seconds)
    public AnimationCurve curve;    // Evaluated [0..1] over the duration
    public float intensityDelta;    // Max intensity added on top of baseline
    public float responseDelta;     // Max response added on top of baseline
    public FilmGrainLookup? grainType;
    // ^ Use "?" if you want to optionally override the type; else you can store a default

    public URPFilmGrainEvent(
        float duration,
        AnimationCurve curve,
        float intensityDelta,
        float responseDelta,
        FilmGrainLookup? grainType = null
    )
    {
        this.duration = duration;
        this.curve = curve;
        this.intensityDelta = intensityDelta;
        this.responseDelta = responseDelta;
        this.grainType = grainType;
    }
}