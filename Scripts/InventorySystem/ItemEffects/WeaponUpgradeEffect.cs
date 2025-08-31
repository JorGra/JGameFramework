using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Adds stacks of one or more WeaponUpgradeDef entries to the player's weapon controller.
    /// - effectType: "WeaponUpgrade"
    /// - effectParams can be:
    ///   { "upgradeId":"MyMod_Upgrade", "stacks": 2 }
    ///   or { "entries":[ {"id":"A","stacks":1}, {"id":"B","stacks":3} ] }
    ///   or simply "MyMod_Upgrade" (stacks=1)
    /// </summary>
    [ItemEffect("WeaponUpgrade")]
    public sealed class WeaponUpgradeEffect : IItemEffect
    {
        private readonly List<Entry> entries = new();

        public WeaponUpgradeEffect(IEnumerable<Entry> items)
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
                for (int i = 0; i < e.stacks; i++) controller.AddUpgradeById(e.id);
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
                for (int i = 0; i < e.stacks; i++) controller.RemoveUpgradeById(e.id);
            }
        }

        // ---------------- Factory ----------------
        public static IItemEffect FromJson(JToken token)
        {
            // Allow a raw string: "upgradeId"
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return new WeaponUpgradeEffect(null);

            if (token.Type == JTokenType.String)
            {
                var id = token.Value<string>();
                return new WeaponUpgradeEffect(new[] { new Entry { id = id, stacks = 1 } });
            }

            var list = new List<Entry>();

            // Object with upgradeId/stacks or entries[]
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                if (obj.TryGetValue("entries", out var arrTok) && arrTok is JArray arr)
                {
                    foreach (var el in arr)
                    {
                        var id = el["id"]?.ToString() ?? el["upgradeId"]?.ToString();
                        int stacks = el["stacks"]?.Value<int?>() ?? 1;
                        if (!string.IsNullOrWhiteSpace(id) && stacks > 0)
                            list.Add(new Entry { id = id, stacks = stacks });
                    }
                }
                else
                {
                    var id = obj["upgradeId"]?.ToString() ?? obj["id"]?.ToString();
                    int stacks = obj["stacks"]?.Value<int?>() ?? 1;
                    if (!string.IsNullOrWhiteSpace(id) && stacks > 0)
                        list.Add(new Entry { id = id, stacks = stacks });
                }
            }

            return new WeaponUpgradeEffect(list);
        }

        static WeaponUpgradeEffect() => ItemEffectRegistry.Register<WeaponUpgradeEffect>(FromJson);

        public struct Entry
        {
            public string id;
            public int stacks;
        }
    }
}

