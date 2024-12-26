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
    public float weaponDamage;
    public GameObject weaponPrefab;
    public WeaponFireMode weaponFireMode;
    public IAttackCommand attackCommand;
    [SerializeReference]
    public List<IWeaponDataComponent> dataComponents = new List<IWeaponDataComponent>();

    public T GetDataComponent<T>() where T : class, IWeaponDataComponent
    {
        foreach (var comp in dataComponents)
        {
            if (comp is T tComp)
                return tComp;
        }
        return null;
    }
}

public interface IWeaponDataComponent
{
    
}
