using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IWeapon
{
    void Initialize(WeaponConfig config);
    void Equip(PlayerStatsController playerStats);
    void Unequip();
    void Use(Transform target, float windUpPower = 1f);
}

public abstract class WeaponBase : MonoBehaviour, IWeapon
{
    protected WeaponConfig config;
    
    public void Initialize(WeaponConfig weaponConfig)
    {
        this.config = weaponConfig;
    }

    public virtual void Equip(PlayerStatsController playerStats)
    {
        Debug.Log("Equipping " + config.weaponName);
    }

    public virtual void Unequip()
    {
        Debug.Log("Unequipping " + config.weaponName);
    }

    public abstract void Use(Transform target, float windUpPower = 1f);
}
