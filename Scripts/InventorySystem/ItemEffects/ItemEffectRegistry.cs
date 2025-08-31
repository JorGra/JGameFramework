using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace JG.Inventory
{
    /// <summary>
    /// Central lookup: effect-id -> factory delegate.
    /// Effects may self-register or just carry <see cref="ItemEffectAttribute"/>.
    /// </summary>
    public static class ItemEffectRegistry
    {
        private static readonly Dictionary<string, Func<JToken, IItemEffect>> factories = new();
        private static bool bootstrapped;

        /// <summary>Called by an effect's static ctor (optional).</summary>
        public static void Register<T>(Func<JToken, IItemEffect> factory)
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
        public static IItemEffect Build(string id, JToken args)
        {
            if (!bootstrapped) Bootstrap();

            if (factories.TryGetValue(id, out var f))
                return f(args);

            Debug.LogError($"[ItemEffectRegistry] Unknown effect id '{id}'.");
            return null;
        }

        /// <summary>One-time reflection pass to auto-register every effect class.</summary>
        private static void Bootstrap()
        {
            bootstrapped = true;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in asm.GetTypes())
                {
                    if (!typeof(IItemEffect).IsAssignableFrom(t) || t.IsAbstract) continue;

                    var attr = t.GetCustomAttribute<ItemEffectAttribute>();
                    if (attr == null || factories.ContainsKey(attr.Id)) continue;

                    // Prefer a public static FromJson(JToken) method
                    var miToken = t.GetMethod("FromJson",
                                               BindingFlags.Public | BindingFlags.Static,
                                               null,
                                               new[] { typeof(JToken) }, null);
                    if (miToken != null)
                    {
                        var delTok = (Func<JToken, IItemEffect>)Delegate.CreateDelegate(
                                         typeof(Func<JToken, IItemEffect>), miToken);
                        factories[attr.Id] = delTok;
                        continue;
                    }

                    Debug.LogWarning($"ItemEffect '{t.Name}' has no FromJson(JToken).");
                }
            }
        }
    }
}
