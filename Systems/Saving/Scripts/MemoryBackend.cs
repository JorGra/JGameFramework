using System;
using System.Collections.Generic;
using System.Linq;

namespace JGameFramework.Saving
{
    public sealed class MemoryBackend : ISaveBackend
    {
        public string Name => "memory";

        private readonly Dictionary<(string slot, string caseId), object> store = new();

        public void Save<T>(string slotId, string caseId, T value)
        {
            store[(slotId, caseId)] = value;
        }

        public T Load<T>(string slotId, string caseId, T defaultValue)
        {
            if (store.TryGetValue((slotId, caseId), out var obj) && obj is T typed)
                return typed;
            return defaultValue;
        }

        public bool Exists(string slotId, string caseId)
        {
            return store.ContainsKey((slotId, caseId));
        }

        public void Delete(string slotId, string caseId)
        {
            store.Remove((slotId, caseId));
        }

        public void DeleteSlot(string slotId)
        {
            var keys = store.Keys
                .Where(k => k.slot.Equals(slotId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keys)
                store.Remove(key);
        }
    }
}
