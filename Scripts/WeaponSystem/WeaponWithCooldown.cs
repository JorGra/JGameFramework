using UnityEngine;

public abstract class WeaponWithCooldown : WeaponBase
{
    protected Cooldown cooldown;
    protected PlayerStatsController playerStats;
    public override void Equip(PlayerStatsController playerStats)
    {
        base.Equip(playerStats);
        this.playerStats = playerStats;
        if(playerStats == null)
        {
            Debug.LogError("PlayerStatsController is null");
            return;
        }
        
        cooldown = new Cooldown(config.GetDataComponent<CooldownWeaponData>().ReloadTime * playerStats.Stats.GetStat(StatType.AttackSpeed));
    }

    protected virtual bool CanUseWeapon()
    {
        return cooldown.CooldownReady();
    }

    public override void Use(Transform target, float windUpPower = 1f)
    {
        cooldown.ResetCooldown(config.GetDataComponent<CooldownWeaponData>().ReloadTime * playerStats.Stats.GetStat(StatType.AttackSpeed));
    }
}