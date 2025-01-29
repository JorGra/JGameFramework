using JG.Tools;
using System.Collections;
using UnityEngine;

public abstract class EffectBase : MonoBehaviour, IEffect
{
    [field: SerializeField]
    public float StartDelay { get; set; } = 0f;

    public virtual void InitEffector()
    {

    }

    /// <summary>
    /// Plays this effect in a coroutine.
    /// If decoupled = false, the caller can yield on the coroutine as normal.
    /// If decoupled = true, the effect is scheduled via the CoroutineRunner,
    /// and returns immediately (caller won't await completion).
    /// </summary>
    public IEnumerator PlayEffect(bool decoupled = false)
    {
        if (!decoupled)
        {
            yield return RunEffectSequence();
        }
        else
        {
            CoroutineRunner.StartCoroutine(RunEffectSequence());
            yield break;
        }
    }

    /// <summary>
    /// The actual effect sequence: start delay -> effect logic -> end delay.
    /// </summary>
    private IEnumerator RunEffectSequence()
    {
        yield return new WaitForSeconds(StartDelay);
        yield return PlayEffectLogic();
    }

    /// <summary>
    /// Override this in subclasses with specific effect behavior.
    /// </summary>
    protected virtual IEnumerator PlayEffectLogic()
    {
        yield return null;
    }
}
