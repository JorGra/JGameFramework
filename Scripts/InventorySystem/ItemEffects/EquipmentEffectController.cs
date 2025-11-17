using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Applies & removes item effects for the *local* Stats instance found via
    /// <see cref="IStatsProvider"/>.
    /// </summary>
    public class EquipmentEffectController : MonoBehaviour
    {
        Stats targetStats;     // auto-discovered
        readonly Dictionary<EquipmentSlot, List<IItemEffect>> active = new();

        Stats TargetStats
        {
            get
            {
                if (targetStats == null)
                    targetStats = (GetComponent<IStatsProvider>() ??
                                   GetComponentInParent<IStatsProvider>())?.Stats;
                return targetStats;
            }
        }

        void OnEnable()
        {
            this.SubscribeEvent<ItemEquippedEvent>(OnEquipped);
            this.SubscribeEvent<ItemUnequippedEvent>(OnUnequipped);
        }

        /* ───────── handlers ───────── */

        void OnEquipped(ItemEquippedEvent e)
        {
            if (!IsMine(e.Context)) return;

            var list = new List<IItemEffect>();
            if (e.Stack.Data.Effects != null)
            {
                foreach (var def in e.Stack.Data.Effects)
                {
                    var fx = def?.BuildEffect();
                    if (fx == null) continue;

                    fx.Apply(e.Context);
                    list.Add(fx);
                }
            }
            else if (e.Stack.Data.LegacyEffects != null)
            {
                foreach (var def in e.Stack.Data.LegacyEffects)
                {
                    var fx = ItemEffectRegistry.Build(def.effectType, def.effectParams);
                    if (fx == null) continue;

                    fx.Apply(e.Context);
                    list.Add(fx);
                }
            }

            if (list.Count > 0)
                active[e.Slot] = list;
        }

        void OnUnequipped(ItemUnequippedEvent e)
        {
            if (!IsMine(e.Context)) return;

            if (active.TryGetValue(e.Slot, out var list))
            {
                foreach (var fx in list) fx.Remove(e.Context);
                active.Remove(e.Slot);
            }
        }

        /* ───────── helpers ───────── */

        bool IsMine(IInventoryContext ctx) =>
            ctx != null && ctx.TryGet<Stats>(out var s) && s == TargetStats;
    }
}
