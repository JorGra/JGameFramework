using UnityEngine;

[CreateAssetMenu(fileName = "WeaponCooldownHandler", menuName = "Gameplay/WeaponSystem/WeaponSubsystem/WeaponCooldownHandler")]
public class WeaponCooldownHandler : WeaponHandlerBase, IWeaponUseageHandler
{
    WeaponConfig config;
    Cooldown Cooldown;
    public bool CanUse()
    {
        return Cooldown.CooldownReady();
    }

    public void Initialize(WeaponConfig config)
    {
        this.config = config;
        Cooldown = new Cooldown(config.GetDataComponent<CooldownWeaponData>().ReloadTime);
    }

    public void Use()
    {
        Cooldown.ResetCooldown(config.GetDataComponent<CooldownWeaponData>().ReloadTime);
    }
}



