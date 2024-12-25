using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class AttackStrategy : ScriptableObject
{
    public abstract void Init();
    public abstract void Execute(Vector3 targetPostition, GameObject effectPrefab);
}


[CreateAssetMenu(menuName = "Gameplay/WeaponSystem/Single Attack Strategy", fileName = "Single Attack Strategy")]
public class SingleAttackStrategy : AttackStrategy
{
    public override void Execute(Vector3 targetPostition, GameObject effectPrefab)
    {
        GameObject spellEffect = Instantiate(effectPrefab, targetPostition, Quaternion.identity);
            
            
        ParticleSystem particleSystem = spellEffect.GetComponent<ParticleSystem>();
        if(particleSystem != null)
        {
            particleSystem.Play();
            Destroy(spellEffect, particleSystem.main.duration);
        }
        else
        {
            Destroy(spellEffect, 2f);
        }

    }

    public override void Init()
    {
    }
}
