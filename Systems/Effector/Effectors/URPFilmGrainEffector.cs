using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPFilmGrainEffector : MonoBehaviour
{
    private Volume volume;
    private FilmGrain filmGrainOverride;

    private float baselineIntensity;
    private float baselineResponse;
    private FilmGrainLookup baselineType;

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

        if (volume.profile.TryGet(out filmGrainOverride))
        {
            baselineIntensity = filmGrainOverride.intensity.value;
            baselineResponse = filmGrainOverride.response.value;
            baselineType = filmGrainOverride.type.value;
        }
    }

    private void OnEnable()
    {
        this.SubscribeEvent<URPFilmGrainEvent>(OnFilmGrainEvent);
    }

    private void OnFilmGrainEvent(URPFilmGrainEvent evt)
    {
        if (volume == null) return;

        if (!volume.profile.TryGet(out filmGrainOverride))
        {
            filmGrainOverride = volume.profile.Add<FilmGrain>(true);
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
        FilmGrainLookup? lastTypeOverride = null;

        for (int i = activeGrains.Count - 1; i >= 0; i--)
        {
            var g = activeGrains[i];
            float elapsed = Time.time - g.startTime;
            if (elapsed >= g.duration)
            {
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

        float finalIntensity = Mathf.Clamp01(baselineIntensity + totalIntensityDelta);
        float finalResponse = Mathf.Max(0f, baselineResponse + totalResponseDelta);

        filmGrainOverride.active = true;
        filmGrainOverride.intensity.value = finalIntensity;
        filmGrainOverride.response.value = finalResponse;
        filmGrainOverride.type.value = lastTypeOverride ?? baselineType;
    }
}

/// <summary>
/// Describes a time-based FilmGrain effect.
/// </summary>
public struct URPFilmGrainEvent : IEvent
{
    public float duration;
    public AnimationCurve curve;
    public float intensityDelta;
    public float responseDelta;
    public FilmGrainLookup? grainType;

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
