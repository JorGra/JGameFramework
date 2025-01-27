using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemEffect : EffectBase
{
    public ParticleSystem ps;
    public Transform spawnTransform;
    /// <summary>
    /// Gets only used if no spawnTransform is assigned
    /// </summary>
    public Vector3 spawnPosition;

    protected override IEnumerator PlayEffectLogic()
    {
        if (ps == null)
        {
            Debug.LogError("ParticleSystemEffect: Particle System is not assigned.");
            yield break;
        }

        if (spawnTransform != null)
            ps.transform.parent = spawnTransform;
        else
            ps.transform.position = spawnPosition;

        ps.Play();
    }
}
