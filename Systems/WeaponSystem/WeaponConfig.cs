using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponFireMode
{
    Single,
    Auto
}

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Gameplay/WeaponSystem/WeaponConfig", order = 1)]
public partial class WeaponConfig : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab;
    public WeaponFireMode weaponFireMode;
    public IAttackCommand attackCommand;
    public StatModifierConfig[] StatsModifiers;
    public WeaponData[] weaponData;
    public InterfaceReference<IWeaponUseageHandler> weaponUseageHandler; //Handle weapon usage, like cooldowns, ammo, etc.
    private Dictionary<System.Type, WeaponData> cachedWeaponData = new();

    public T GetDataComponent<T>() where T : WeaponData 
    {

        if (cachedWeaponData.TryGetValue(typeof(T), out WeaponData data))
            return (T)data;

        foreach (var comp in weaponData)
        {
            if (comp is T tComp)
            {
                cachedWeaponData.Add(typeof(T), tComp);
                return tComp;
            }
        }
        Debug.LogError("No component of type " + typeof(T) + " found in weaponData");
        return null;
    }
}


public abstract class WeaponData : ScriptableObject
{

}

public interface IWeaponUseageHandler
{
    void Initialize(WeaponConfig config);
    bool CanUse();
    public void Use();
}

public abstract class WeaponHandlerBase : ScriptableObject
{
    
}



