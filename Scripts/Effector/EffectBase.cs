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

    public IEnumerator PlayEffect()
    {
        yield return new WaitForSeconds(StartDelay);
        yield return PlayEffectLogic();
        yield return new WaitForSeconds(EndDelay);
    }

    protected virtual IEnumerator PlayEffectLogic()
    {
        yield return null;
    }
}
