using UnityEngine;

[CreateAssetMenu(fileName = "WeaponCooldownHandler", menuName = "Gameplay/WeaponSystem/WeaponSubsystem/WeaponCooldownHandler")]
public class WeaponCooldownHandler : WeaponHandlerBase, IWeaponUseageHandler
{
    WeaponConfig config;
    Cooldown Cooldown;
    public bool CanUse()
    {
        return Cooldown.Ready();
    }

    public void Initialize(WeaponConfig config)
    {
        this.config = config;
        Cooldown = new Cooldown(1);
    }

    public void Use()
    {
        Cooldown.Reset(1);
    }
}



