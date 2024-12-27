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

    public T GetDataComponent<T>() where T : WeaponData 
    {
        foreach (var comp in weaponData)
        {
            if (comp is T tComp)
                return tComp;
        }
        Debug.LogError("No component of type " + typeof(T) + " found in weaponData");
        return null;
    }
}


public abstract class WeaponData : ScriptableObject
{

}



