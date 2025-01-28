using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectorController : MonoBehaviour
{
    public List<IEffect> effects = new List<IEffect>();

    IEnumerator PlayEffects()
    {
        foreach (var effect in effects)
        {
            yield return StartCoroutine(effect.PlayEffect());
        }
    }

    public void Play() => StartCoroutine(PlayEffects());

    public void AddEffect(IEffect effect)
    {
        effects.Add(effect);
    }

    public void RemoveEffect(IEffect effect)
    {
        effects.Remove(effect);
    }


}
