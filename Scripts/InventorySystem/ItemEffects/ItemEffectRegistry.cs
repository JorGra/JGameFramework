using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Central lookup table: effect-id → factory delegate.
    /// Effects register themselves in their static ctor.
    /// </summary>
    public static class ItemEffectRegistry
    {
        private static readonly Dictionary<string, Func<string, IItemEffect>> factories = new();

        /// <summary>Called by an effect's static constructor.</summary>
        public static void Register<T>(Func<string, IItemEffect> factory)
            where T : IItemEffect
        {
            var attr = typeof(T).GetCustomAttribute<ItemEffectAttribute>();
            if (attr == null)
            {
                Debug.LogError($"{typeof(T).Name} is missing [ItemEffect] attribute.");
                return;
            }

            factories[attr.Id] = factory;
        }

        /// <summary>Creates the effect; returns <c>null</c> if id is unknown.</summary>
        public static IItemEffect Build(string id, string json)
        {
            if (factories.TryGetValue(id, out var f))
                return f(json);

            Debug.LogError($"[ItemEffectRegistry] Unknown effect id '{id}'.");
            return null;
        }
    }
}
