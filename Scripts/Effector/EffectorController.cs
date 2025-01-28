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

    IEnumerator PlayEffects(bool decoupled = false)
    {
        foreach (var effect in effects)
        {
            yield return StartCoroutine(effect.PlayEffect(decoupled));
        }
    }

    public void Play()
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(PlayEffects());
    }

    public void PlayDecoupled()
    {
        CoroutineRunner.StartCoroutine(PlayEffects(true));
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
