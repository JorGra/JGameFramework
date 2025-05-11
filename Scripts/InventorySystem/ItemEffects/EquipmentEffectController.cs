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

        EventBinding<ItemEquippedEvent> bindEq;
        EventBinding<ItemUnequippedEvent> bindUnEq;

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
            bindEq = new EventBinding<ItemEquippedEvent>(OnEquipped);
            bindUnEq = new EventBinding<ItemUnequippedEvent>(OnUnequipped);
            EventBus<ItemEquippedEvent>.Register(bindEq);
            EventBus<ItemUnequippedEvent>.Register(bindUnEq);
        }

        void OnDisable()
        {
            EventBus<ItemEquippedEvent>.Deregister(bindEq);
            EventBus<ItemUnequippedEvent>.Deregister(bindUnEq);
        }

        /* ───────── handlers ───────── */

        void OnEquipped(ItemEquippedEvent e)
        {
            if (!IsMine(e.Context)) return;

            var list = new List<IItemEffect>();
            foreach (var def in e.Stack.Data.Effects)
            {
                var fx = ItemEffectRegistry.Build(def.effectType, def.effectParams);
                if (fx == null) continue;

                fx.Apply(e.Context);
                list.Add(fx);
            }
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

        bool IsMine(InventoryContext ctx) =>
            ctx != null && ctx.TargetStats == TargetStats;
    }
}
