using JG.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EffectBase : MonoBehaviour, IEffect
{
    [field: SerializeField]
    public float StartDelay { get; set; } = 0f;

    [field: SerializeField]
    public float EndDelay { get; set; } = 0f;
    public virtual void InitEffector()
    {

    }
    public IEnumerator PlayEffect(bool decoupled = false)
    {
        yield return new WaitForSeconds(StartDelay);

        if (decoupled)
            yield return CoroutineRunner.StartCoroutine(PlayEffectLogic());
        else
            yield return PlayEffectLogic();

        yield return new WaitForSeconds(EndDelay);
    }

    protected virtual IEnumerator PlayEffectLogic()
    {
        yield return null;
    }
}
