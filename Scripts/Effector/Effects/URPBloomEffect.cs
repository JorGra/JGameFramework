using System.Collections;
using UnityEngine;

public class URPBloomEffect : EffectBase
{
    [Header("URP Bloom Settings")]
    [SerializeField] private float baseIntensity = 1f;
    [SerializeField] private float duration = 2f;

    [Tooltip("Curve to scale intensity from 0..1 over the duration.")]
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [SerializeField] private float threshold = 1f;
    [SerializeField] private float scatter = 0.7f;
    [SerializeField] private Color tintColor = Color.white;
    [SerializeField] private bool bloomEnabled = true;

    protected override IEnumerator PlayEffectLogic()
    {
        // Raise the event so a URPBloomEffector can handle the actual bloom
        EventBus<URPBloomEvent>.Raise(new URPBloomEvent(
            baseIntensity,
            duration,
            intensityCurve,
            threshold,
            scatter,
            tintColor,
            bloomEnabled
        ));

        yield return null; // The effect is driven externally in the effector
    }
}
