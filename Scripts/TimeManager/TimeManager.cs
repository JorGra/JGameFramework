using UnityEngine;
using System.Collections.Generic;

public class TimeManager : MonoBehaviour
{
    // All active time effects
    private List<TimeEffect> activeEffects = new List<TimeEffect>();

    // The final time scale we are *currently* applying (for smooth transition).
    // We'll smoothly lerp from `Time.timeScale` to `desiredTimeScale`.
    private float desiredTimeScale = 1f;

    // If multiple effects have the same priority, decide tie-break rule:
    // - "lowest time-scale" = pick the effect that results in the slowest time
    // - "highest time-scale" = pick the effect that results in the fastest time
    // - "newest" = pick the effect that was added last among those with the highest priority
    // - etc.
    public enum TieBreakRule { LowestTimeScale, HighestTimeScale, Newest }
    public TieBreakRule tieBreakRule = TieBreakRule.LowestTimeScale;

    // If you want a global smoothing speed for transitions (instead of per effect),
    // set something here. If you prefer each effect to define its own speed, read it from the effect.
    public float globalLerpSpeed = 5f;


    private IEventBinding<TimeFlowEvent> timeFlowBinding;

    void OnEnable()
    {
        // Register for time flow events
        timeFlowBinding = new EventBinding<TimeFlowEvent>(OnTimeFlowEvent);
        EventBus<TimeFlowEvent>.Register(timeFlowBinding);
    }

    void OnDisable()
    {
        EventBus<TimeFlowEvent>.Deregister(timeFlowBinding);
    }

    private void OnTimeFlowEvent(TimeFlowEvent evt)
    {
        if (evt.startEffect)
        {
            // Start or update
            TimeEffect newEff = new TimeEffect(
                evt.effectName,
                evt.priority,
                evt.targetTimeScale,
                evt.duration,
                evt.lerpSpeed
            );
            AddEffect(newEff);
        }
        else
        {
            // Stop
            StopEffect(evt.effectName);
        }
    }

    void Update()
    {
        // 1) Remove or deactivate any effects that have expired
        float now = Time.unscaledTime; // unscaled time so it isn't affected by timeScale
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            TimeEffect eff = activeEffects[i];
            // If it has a nonzero duration and we've gone past it
            if (eff.isActive && eff.duration > 0f && (now - eff.startTime) >= eff.duration)
            {
                // Mark it inactive
                eff.isActive = false;
            }
        }

        // 2) Remove or filter out inactive effects
        activeEffects.RemoveAll(e => !e.isActive);

        // 3) Pick the highest-priority effect
        if (activeEffects.Count == 0)
        {
            // No effects => default timescale is 1
            desiredTimeScale = 1f;
        }
        else
        {
            // Sort or pick the effect with the highest priority
            TimeEffect best = FindHighestPriorityEffect(activeEffects);
            desiredTimeScale = best.targetTimeScale;
        }

        // 4) Smoothly approach the desiredTimeScale
        float currentTS = Time.timeScale;
        // Option A: Use the effect's own lerpSpeed if you want the chosen effect's speed
        // Option B: Use a single global speed
        // We'll do a simple approach with global speed here
        float finalTS = Mathf.MoveTowards(currentTS, desiredTimeScale, globalLerpSpeed * Time.unscaledDeltaTime);
        Time.timeScale = finalTS;
    }

    /// <summary>
    /// Add or update an effect in the manager. If effectName matches an existing effect, we can replace it.
    /// Otherwise, create a new entry.
    /// </summary>
    public void AddEffect(TimeEffect newEffect)
    {
        // If we already have an effect with the same name, update it
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].effectName == newEffect.effectName)
            {
                activeEffects[i].priority = newEffect.priority;
                activeEffects[i].targetTimeScale = newEffect.targetTimeScale;
                activeEffects[i].duration = newEffect.duration;
                activeEffects[i].lerpSpeed = newEffect.lerpSpeed;
                activeEffects[i].startTime = Time.unscaledTime;
                activeEffects[i].isActive = true;
                return;
            }
        }

        // Otherwise, new effect
        newEffect.startTime = Time.unscaledTime;
        newEffect.isActive = true;
        activeEffects.Add(newEffect);
    }

    /// <summary>
    /// Stop (remove) an effect by name. If found, it gets removed from the manager.
    /// </summary>
    public void StopEffect(string effectName)
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i].effectName == effectName)
            {
                activeEffects[i].isActive = false;
            }
        }
    }

    /// <summary>
    /// Finds the highest priority effect. If there's a tie in priority, we apply tieBreakRule.
    /// </summary>
    private TimeEffect FindHighestPriorityEffect(List<TimeEffect> effects)
    {
        TimeEffect best = null;
        foreach (var eff in effects)
        {
            if (best == null)
            {
                best = eff;
            }
            else
            {
                // Compare priorities
                if (eff.priority > best.priority)
                {
                    best = eff;
                }
                else if (eff.priority == best.priority)
                {
                    // tie-break
                    switch (tieBreakRule)
                    {
                        case TieBreakRule.LowestTimeScale:
                            if (eff.targetTimeScale < best.targetTimeScale)
                                best = eff;
                            break;
                        case TieBreakRule.HighestTimeScale:
                            if (eff.targetTimeScale > best.targetTimeScale)
                                best = eff;
                            break;
                        case TieBreakRule.Newest:
                            // The one with the more recent startTime is "newer"
                            if (eff.startTime > best.startTime)
                                best = eff;
                            break;
                    }
                }
            }
        }
        return best;
    }
}

/// <summary>
/// Raised on the event bus to control time flow. 
/// If 'startEffect == true', we add/update an effect with specified parameters.
/// If 'startEffect == false', we stop the effect by name.
/// </summary>
public struct TimeFlowEvent : IEvent
{
    public bool startEffect;
    public string effectName;
    public int priority;
    public float targetTimeScale;
    public float duration;
    public float lerpSpeed;

    public TimeFlowEvent(bool startEffect, string effectName, int priority, float targetTimeScale, float duration, float lerpSpeed)
    {
        this.startEffect = startEffect;
        this.effectName = effectName;
        this.priority = priority;
        this.targetTimeScale = targetTimeScale;
        this.duration = duration;
        this.lerpSpeed = lerpSpeed;
    }
}