using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemEffect : EffectBase
{
    public ParticleSystem particleSystem;
    public Transform spawnTransform;
    /// <summary>
    /// Gets only used if no spawnTransform is assigned
    /// </summary>
    public Vector3 spawnPosition;
    
    /// <summary>
    /// Should the particle system be parented to the spawntransform? 
    /// useful for moving objects
    /// </summary>
    bool parentParticleSystem = false;


    protected override IEnumerator PlayEffectLogic()
    {
        if (particleSystem == null)
        {
            Debug.LogError("ParticleSystemEffect: Particle System is not assigned.");
            yield break;
        }

        if (spawnTransform != null)
        {
            particleSystem.transform.position = spawnTransform.position;
            if (parentParticleSystem)
            {
                particleSystem.transform.parent = spawnTransform;
            }
        }
        else
            particleSystem.transform.position = spawnPosition;

        particleSystem.Play();
    }
}
