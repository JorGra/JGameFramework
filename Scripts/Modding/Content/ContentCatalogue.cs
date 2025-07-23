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

        public void AddOrReplace<T>(T def) where T : class, IContentDef
        {
            var map = _tables.GetOrAdd(typeof(T),
                _ => new ConcurrentDictionary<string, IContentDef>(
                    StringComparer.OrdinalIgnoreCase));

            map[def.Id] = def; // last‑write‑wins
            map[def.Id].SourceFile = def.SourceFile; // update source file
        }
        public void AddOrReplace(IContentDef def)
        {
            // Store under the concrete runtime type
            var t = def.GetType();
            var map = _tables.GetOrAdd(t,
                _ => new ConcurrentDictionary<string, IContentDef>(
                         StringComparer.OrdinalIgnoreCase));

            map[def.Id] = def;                    // last‑write‑wins
            map[def.Id].SourceFile = def.SourceFile;
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
