using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace JG.GameContent.Localization
{
    /// <summary>
    /// Builds and caches Unity Localization <see cref="StringTable"/> instances
    /// from the translations loaded by <see cref="ModTranslationLoader"/>.
    /// This allows code that queries <c>LocalizationSettings.StringDatabase</c>
    /// for the <c>"ModContent"</c> table to receive mod translations.
    /// </summary>
    public sealed class ModStringTableProvider
    {
        public const string TableName = "ModContent";

        // locale code → cached StringTable
        readonly Dictionary<string, StringTable> _cache = new();

        /// <summary>
        /// Get (or build) the <see cref="StringTable"/> for the given locale.
        /// Returns <c>null</c> when no translations exist for that locale.
        /// </summary>
        public StringTable GetTable(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode))
                return null;

            if (_cache.TryGetValue(localeCode, out var table))
                return table;

            var entries = ModTranslationLoader.Instance.GetLocale(localeCode);
            if (entries == null || entries.Count == 0)
                return null;

            table = ScriptableObject.CreateInstance<StringTable>();
            table.SharedData = ScriptableObject.CreateInstance<SharedTableData>();
            table.SharedData.TableCollectionName = TableName;

            foreach (var kv in entries)
                table.AddEntry(kv.Key, kv.Value);

            _cache[localeCode] = table;
            return table;
        }

        /// <summary>
        /// Look up a single translated entry for the current locale.
        /// Returns <c>null</c> when no translation is found.
        /// </summary>
        public string GetEntry(string key)
        {
            var localeCode = LocalizationExtensions.CurrentLocaleCode;
            if (string.IsNullOrEmpty(localeCode))
                return null;

            var table = GetTable(localeCode);
            var entry = table?.GetEntry(key);
            return entry?.Value;
        }

        /// <summary>
        /// Invalidate the cache so the next lookup rebuilds tables.
        /// Call after translations are reloaded.
        /// </summary>
        public void InvalidateCache()
        {
            foreach (var table in _cache.Values)
            {
                if (table != null)
                    Object.Destroy(table);
            }
            _cache.Clear();
        }
    }
}
