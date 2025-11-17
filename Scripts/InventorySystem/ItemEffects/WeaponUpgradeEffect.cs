using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Weapons;

namespace JG.Inventory
{
    /// <summary>
    /// Adds stacks of one or more WeaponUpgradeDef entries to the player's weapon controller.
    /// - effectType: "WeaponUpgrade"
    /// - effectParams can be:
    ///   { "upgradeId":"MyMod_Upgrade", "stacks": 2 }
    ///   or { "entries":[ {"id":"A","stacks":1}, {"id":"B","stacks":3} ] }
    ///   or simply "MyMod_Upgrade" (stacks=1)
    ///   Optional "group" value can be "Primary", "Secondary" or "All".
    /// </summary>
    [ItemEffect("WeaponUpgrade")]
    public sealed class WeaponUpgradeEffect : IItemEffect
    {
        private readonly List<WeaponUpgradeEffectDef.Entry> entries = new();

        public WeaponUpgradeEffect(IEnumerable<WeaponUpgradeEffectDef.Entry> items)
        {
            if (items != null) entries.AddRange(items);
        }

        public void Apply(IInventoryContext ctx)
        {
            if (!ctx.TryGet<Player>(out var player))
            {
                Debug.LogWarning("[WeaponUpgradeEffect] No Player in context; cannot apply upgrade.");
                return;
            }
            var controller = player.GetComponent<PlayerWeaponController>();
            if (controller == null)
            {
                Debug.LogWarning("[WeaponUpgradeEffect] PlayerWeaponController not found on Player.");
                return;
            }

            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.id) || e.stacks <= 0) continue;
                var target = e.targetOverride ?? WeaponUpgradeTarget.All;
                for (int i = 0; i < e.stacks; i++) controller.AddUpgradeById(e.id, target);
            }
        }

        public void Remove(IInventoryContext ctx)
        {
            if (!ctx.TryGet<Player>(out var player)) return;
            var controller = player.GetComponent<PlayerWeaponController>();
            if (controller == null) return;

            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.id) || e.stacks <= 0) continue;
                var target = e.targetOverride ?? WeaponUpgradeTarget.All;
                for (int i = 0; i < e.stacks; i++) controller.RemoveUpgradeById(e.id, target);
            }
        }

    }
}

