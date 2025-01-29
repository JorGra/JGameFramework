using System.Collections;
using UnityEngine;

/// <summary>
/// Sends a TimeFlowEvent to your TimeManager, slowing time for a certain duration or indefinitely.
/// Configurable via the Inspector: effect name, priority, target timescale, etc.
/// </summary>
public class TimeSlowEffect : EffectBase
{
    [Header("Time Slow Settings")]
    [SerializeField] private string effectName = "TimeSlowEffect";
    [SerializeField] private int priority = 50;
    [SerializeField] private float targetTimeScale = 0.5f;
    [Tooltip("Duration in seconds. 0 => indefinite until stopped manually.")]
    [SerializeField] private float duration = 3f;
    [Tooltip("How quickly we interpolate timescale changes. Higher = faster transition.")]
    [SerializeField] private float lerpSpeed = 5f;
    [Tooltip("If true, we automatically 'stop' this effect when the duration ends (if duration > 0).")]
    [SerializeField] private bool autoStopWhenFinished = true;

    protected override IEnumerator PlayEffectLogic()
    {
        // 1) Raise an event to start/update the time slow effect
        var startEvent = new TimeFlowEvent(
            startEffect: true,
            effectName: effectName,
            priority: priority,
            targetTimeScale: targetTimeScale,
            duration: duration,
            lerpSpeed: lerpSpeed
        );
        EventBus<TimeFlowEvent>.Raise(startEvent);

        // 2) If we have a duration > 0 and autoStop is enabled, 
        //    wait that long and then raise an event to stop it
        if (duration > 0f && autoStopWhenFinished)
        {
            yield return new WaitForSeconds(duration);

            // Stop the effect by name
            var stopEvent = new TimeFlowEvent(
                startEffect: false,
                effectName: effectName,
                priority: 0,
                targetTimeScale: 0f,
                duration: 0f,
                lerpSpeed: 0f
            );
            EventBus<TimeFlowEvent>.Raise(stopEvent);
        }
    }
}
