using JG.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectorController : MonoBehaviour
{
    public List<EffectBase> effects = new List<EffectBase>();

    private void Start()
    {
        foreach (var effect in effects)
        {
            effect.InitEffector();
        }
    }

    private IEnumerator PlayEffectsCoroutine(bool decoupled = false)
    {
        // If decoupled = false, we start all effects locally on this GameObject.
        // If decoupled = true, we tell each effect to run on the CoroutineRunner.

        if (decoupled)
        {
            // Decoupled: just fire them all off on the runner with no blocking here.
            foreach (var effect in effects)
            {
                // This schedules each effect on the runner, so we don't block at all
                effect.PlayEffect(true);
            }
            yield break; // Return immediately, as we won't wait for them
        }
        else
        {
            // Coupled: run them *all* in parallel on this GameObject, 
            // then optionally wait for them to finish before returning.
            var runningCoroutines = new List<Coroutine>();
            foreach (var effect in effects)
            {
                // Start each effect locally, store the Coroutine
                var routine = StartCoroutine(effect.PlayEffect(false));
                runningCoroutines.Add(routine);
            }

            // Wait for all coroutines to complete
            foreach (var routine in runningCoroutines)
            {
                yield return routine;
            }
        }
    }

    /// <summary>
    /// Runs all effects *in parallel* on this GameObject. 
    /// The coroutine blocks until all effects have finished.
    /// </summary>
    public void Play()
    {
        if (gameObject != null && gameObject.activeInHierarchy)
            StartCoroutine(PlayEffectsCoroutine(false));
    }

    /// <summary>
    /// Runs all effects decoupled (on the CoroutineRunner). 
    /// Returns immediately, does *not* block for completion.
    /// </summary>
    public void PlayDecoupled()
    {
        // Schedule all effects on the persistent runner. 
        // This EffectorController won't wait for them.
        CoroutineRunner.StartCoroutine(PlayEffectsCoroutine(true));
    }

    public void AddEffect(EffectBase effect)
    {
        effects.Add(effect);
    }

    public void RemoveEffect(EffectBase effect)
    {
        effects.Remove(effect);
    }
}
