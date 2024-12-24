using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IWeapon
{
    void Initialize(WeaponConfig config);
    void Equip();
    void Unequip();
    void Use(Transform transform);
}

public abstract class WeaponBase : MonoBehaviour, IWeapon
{
    protected WeaponConfig config;

    public void Initialize(WeaponConfig weaponConfig)
    {
        this.config = weaponConfig;
    }

    public virtual void Equip()
    {
        Debug.Log("Equipping " + config.weaponName);
    }

    public virtual void Unequip()
    {
        Debug.Log("Unequipping " + config.weaponName);
    }

    public abstract void Use(Transform transform);
}