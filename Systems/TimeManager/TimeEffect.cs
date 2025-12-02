using UnityEngine;

/// <summary>
/// Represents a single time-scale modification, e.g. a crash slowdown or a pause.
/// </summary>
[System.Serializable]
public class TimeEffect
{
    public string effectName;   // e.g. "CrashSlowdown" or "PauseMenu"
    public int priority;        // Higher means more important. E.g. pause might be 100, normal slow could be 50
    public float targetTimeScale;   // E.g. 0.1 for slowdown, 0.0 for full pause, 1.0 for normal
    public float duration;          // 0 => indefinite, or X seconds
    public float startTime;         // When we activated (set by TimeManager)
    public bool isActive = true;    // If it's not stopped or expired

    // For smoothing from the *current* timescale to the target
    // We'll have a short optional "lerp speed" if you want each effect 
    // to define how quickly it takes over. Or you can keep a single speed in TimeManager.
    public float lerpSpeed = 5f;  // e.g. how fast we approach target timescale

    public TimeEffect(string name, int priority, float targetTS, float duration, float lerpSpeed)
    {
        this.effectName = name;
        this.priority = priority;
        this.targetTimeScale = targetTS;
        this.duration = duration;
        this.lerpSpeed = lerpSpeed;
    }
}
