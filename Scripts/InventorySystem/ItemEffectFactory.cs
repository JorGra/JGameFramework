using System;
using System.Collections.Generic;
using System.Reflection;

namespace JG.Inventory
{
    /// <summary>
    /// Maps string names to concrete <see cref="IItemEffect"/> constructors.
    /// </summary>
    public static class ItemEffectFactory
    {
        private static readonly Dictionary<string, MethodInfo> registry = new();

        static ItemEffectFactory()
        {
            // Scan assembly once; register static CreateFromJson(string) methods.
            foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!typeof(IItemEffect).IsAssignableFrom(t)) continue;
                MethodInfo m = t.GetMethod("CreateFromJson", BindingFlags.Public | BindingFlags.Static);
                if (m != null && m.ReturnType == typeof(IItemEffect))
                    registry[t.Name] = m;
            }
        }

        public static IItemEffect Build(ItemEffectDefinition def)
        {
            if (registry.TryGetValue(def.effectType, out MethodInfo factory))
            {
                return (IItemEffect)factory.Invoke(null, new object[] { def.effectParams });
            }

            UnityEngine.Debug.LogError($"[ItemEffectFactory] Unknown effect '{def.effectType}'.");
            return null;
        }
    }
}
