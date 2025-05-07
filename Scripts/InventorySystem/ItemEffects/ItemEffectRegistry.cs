using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Central lookup: effect-id → factory delegate.
    /// Effects may self-register *or* just carry <see cref="ItemEffectAttribute"/>.
    /// </summary>
    public static class ItemEffectRegistry
    {
        static readonly Dictionary<string, Func<string, IItemEffect>> factories = new();
        static bool bootstrapped;

        /* ───────── public API ───────── */

        /// <summary>Called by an effect’s static ctor (optional).</summary>
        public static void Register<T>(Func<string, IItemEffect> factory)
            where T : IItemEffect
        {
            var attr = typeof(T).GetCustomAttribute<ItemEffectAttribute>();
            if (attr == null)
            {
                Debug.LogError($"{typeof(T).Name} missing [ItemEffect] attribute.");
                return;
            }
            factories[attr.Id] = factory;
        }

        /// <summary>
        /// Creates the effect or returns <c>null</c> if the id is unknown.
        /// </summary>
        public static IItemEffect Build(string id, string json)
        {
            if (!bootstrapped) Bootstrap();                 // NEW

            if (factories.TryGetValue(id, out var f))
                return f(json);

            Debug.LogError($"[ItemEffectRegistry] Unknown effect id '{id}'.");
            return null;
        }

        /* ───────── internal ───────── */

        /// <summary>One-time reflection pass to auto-register every effect class.</summary>
        static void Bootstrap()
        {
            bootstrapped = true;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in asm.GetTypes())
                {
                    if (!typeof(IItemEffect).IsAssignableFrom(t) || t.IsAbstract) continue;

                    var attr = t.GetCustomAttribute<ItemEffectAttribute>();
                    if (attr == null || factories.ContainsKey(attr.Id)) continue;

                    /* find a public static FromJson(string) method */
                    var mi = t.GetMethod("FromJson",
                                         BindingFlags.Public | BindingFlags.Static,
                                         null,
                                         new[] { typeof(string) }, null);
                    if (mi == null)
                    {
                        Debug.LogWarning($"ItemEffect '{t.Name}' has no FromJson(string).");
                        continue;
                    }

                    var del = (Func<string, IItemEffect>)Delegate.CreateDelegate(
                                  typeof(Func<string, IItemEffect>), mi);
                    factories[attr.Id] = del;
                }
            }
        }
    }
}
