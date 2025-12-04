using System.Collections;
using UnityEngine;

/// <summary>
/// CameraShakeEffect raises a CameraShakeEvent via your event bus.
/// The actual shake logic is performed in CameraShakeEffector on the camera.
/// </summary>
public class CameraShakeEffect : EffectBase
{
    [Header("Camera Shake Settings")]
    [Tooltip("The maximum shake intensity (e.g. how far camera moves from original pos).")]
    [SerializeField] private float intensity = 0.5f;

    [Tooltip("The frequency (in seconds) between shake steps. E.g. 0.02f -> 50 FPS jitter.")]
    [SerializeField] private float frequency = 0.02f;

    [Tooltip("How long the shake lasts in seconds.")]
    [SerializeField] private float duration = 0.5f;

    protected override IEnumerator PlayEffectLogic()
    {
        // Raise an event that the CameraShakeEffector will receive
        EventBus<CameraShakeEvent>.Raise(new CameraShakeEvent(intensity, frequency, duration));
        yield return null; // no direct wait here, the camera effector does the actual shaking
    }
}

/// <summary>
/// The event data that the camera effector uses to shake the camera.
/// </summary>
public struct CameraShakeEvent : IEvent
{
    public float Intensity { get; private set; }
    public float Frequency { get; private set; }
    public float Duration { get; private set; }

    public CameraShakeEvent(float intensity, float frequency, float duration)
    {
        Intensity = intensity;
        Frequency = frequency;
        Duration = duration;
    }
}
