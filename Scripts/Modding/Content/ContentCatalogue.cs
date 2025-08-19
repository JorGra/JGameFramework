using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace JG.GameContent
{
    /// <summary>
    /// Thread‑safe registry containing the single authoritative instance of every
    /// <see cref="IContentDef"/> in the game. Later mods override earlier ones.
    /// </summary>
    public sealed class ContentCatalogue
    {
        private static readonly Lazy<ContentCatalogue> _lazy =
            new(() => new ContentCatalogue());

        public static ContentCatalogue Instance => _lazy.Value;

        // 1st key = concrete Def type; 2nd key = ID (case‑insensitive)
        private readonly ConcurrentDictionary<Type,
            ConcurrentDictionary<string, IContentDef>> _tables = new();

        private ContentCatalogue() { } // singleton

        // Register under every base type of the object
        private void RegisterUnder(Type key, IContentDef def)
        {
            var map = _tables.GetOrAdd(key,
                _ => new ConcurrentDictionary<string, IContentDef>(StringComparer.OrdinalIgnoreCase));

            map[def.Id] = def; // last-write-wins
            map[def.Id].SourceFile = def.SourceFile; // update source file
        }

        // This method is used for adding content definitions under every type up the inheritance chain.
        public void AddOrReplace(IContentDef def)
        {
            var t = def.GetType();
            while (t != null && typeof(IContentDef).IsAssignableFrom(t))
            {
                RegisterUnder(t, def);
                t = t.BaseType; // Move up the inheritance chain
            }
        }


        public bool TryGet<T>(string id, out T def) where T : class, IContentDef
        {
            def = null;
            if (_tables.TryGetValue(typeof(T), out var map) &&
                map.TryGetValue(id, out var raw))
            {
                def = raw as T;
                return true;
            }
            return false;
        }

        public IEnumerable<T> GetAll<T>() where T : class, IContentDef
        {
            if (_tables.TryGetValue(typeof(T), out var map))
                foreach (var kv in map.Values)
                    yield return kv as T;
        }

        /// <summary>Clear everything – used for hot‑reload in the editor.</summary>
        public void Clear() => _tables.Clear();
    }
}
