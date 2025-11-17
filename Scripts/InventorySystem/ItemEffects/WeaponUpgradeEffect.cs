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

        // ---------------- Factory ----------------
        public static IItemEffect FromJson(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return new WeaponUpgradeEffect(null);

            if (token.Type == JTokenType.String)
            {
                var id = token.Value<string>();
                return new WeaponUpgradeEffect(new[] { new WeaponUpgradeEffectDef.Entry { id = id, stacks = 1 } });
            }

            var list = new List<WeaponUpgradeEffectDef.Entry>();

            // Object with upgradeId/stacks or entries[]
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                if (obj.TryGetValue("entries", out var arrTok) && arrTok is JArray arr)
                {
                    foreach (var el in arr)
                    {
                        var entry = ParseEntry(el as JObject);
                        if (!string.IsNullOrWhiteSpace(entry.id) && entry.stacks > 0)
                            list.Add(entry);
                    }
                }
                else
                {
                    var entry = ParseEntry(obj);
                    if (!string.IsNullOrWhiteSpace(entry.id) && entry.stacks > 0)
                        list.Add(entry);
                }
            }

            return new WeaponUpgradeEffect(list);
        }

        private static WeaponUpgradeEffectDef.Entry ParseEntry(JObject obj)
        {
            if (obj == null) return default;
            var id = obj["upgradeId"]?.ToString() ?? obj["id"]?.ToString();
            int stacks = obj["stacks"]?.Value<int?>() ?? 1;
            var group = obj["group"]?.ToString();
            return new WeaponUpgradeEffectDef.Entry
            {
                id = id,
                stacks = stacks,
                targetOverride = ParseTarget(group)
            };
        }

        private static WeaponUpgradeTarget? ParseTarget(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            switch (value.Trim().ToLowerInvariant())
            {
                case "primary": return WeaponUpgradeTarget.Primary;
                case "secondary": return WeaponUpgradeTarget.Secondary;
                case "all": return WeaponUpgradeTarget.All;
                default:
                    Debug.LogWarning($"[WeaponUpgradeEffect] Unknown upgrade group '{value}'.");
                    return null;
            }
        }

        static WeaponUpgradeEffect() => ItemEffectRegistry.Register<WeaponUpgradeEffect>(FromJson);

    }
}

