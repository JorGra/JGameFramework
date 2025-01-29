using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class URPFilmGrainEffect : EffectBase
{
    [Header("Film Grain Settings")]
    [Tooltip("Effect duration in seconds.")]
    [SerializeField] private float duration = 2f;

    [Tooltip("Curve (0..1) for intensity over time.")]
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Tooltip("How much to add to baseline intensity at the peak.")]
    [SerializeField] private float intensityDelta = 0.5f;

    [Tooltip("How much to add to baseline response at the peak.")]
    [SerializeField] private float responseDelta = 0.5f;

    [Tooltip("Optional override for the film grain type (leave None if no override).")]
    public FilmGrainLookup grainTypeOverride = FilmGrainLookup.Thin1;
    [Tooltip("Whether to override the type at all.")]
    public bool overrideType = false;

    protected override IEnumerator PlayEffectLogic()
    {
        // Construct our event
        URPFilmGrainEvent filmGrainEvent = new URPFilmGrainEvent(
            duration,
            intensityCurve,
            intensityDelta,
            responseDelta,
            overrideType ? (FilmGrainLookup?)grainTypeOverride : null
        );

        // Raise the event
        EventBus<URPFilmGrainEvent>.Raise(filmGrainEvent);

        yield return null;
    }
}
