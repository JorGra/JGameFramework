using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JG.GameContent.Localization
{
    /// <summary>
    /// Reads <c>Translations/*.json</c> sidecar files from each mod folder and
    /// merges them into a single lookup: locale → key → translated text.
    /// Later mods override translations from earlier mods (last-write-wins).
    /// </summary>
    public sealed class ModTranslationLoader
    {
        static readonly Lazy<ModTranslationLoader> _lazy = new(() => new ModTranslationLoader());
        public static ModTranslationLoader Instance => _lazy.Value;

        // locale code → (translation key → translated text)
        readonly Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>All locale codes that have at least one translation loaded.</summary>
        public IEnumerable<string> LoadedLocales => _translations.Keys;

        ModTranslationLoader() { }

        /// <summary>
        /// Scans <c>{modPath}/Translations/*.json</c> and merges every file into
        /// the in-memory table. Call once per mod, in load order.
        /// </summary>
        public void LoadFromMod(string modPath)
        {
            var translationsDir = Path.Combine(modPath, "Translations");
            if (!Directory.Exists(translationsDir))
                return;

            foreach (var file in Directory.GetFiles(translationsDir, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    LoadFile(file);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Localization] Failed to load translation file {file}: {ex.Message}");
                }
            }
        }

        void LoadFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var obj = JObject.Parse(json);

            // Determine language from $language metadata or fall back to file name
            string lang = null;
            if (obj.TryGetValue("$language", out var langToken))
                lang = langToken.Value<string>();

            if (string.IsNullOrWhiteSpace(lang))
                lang = Path.GetFileNameWithoutExtension(filePath);

            if (string.IsNullOrWhiteSpace(lang))
                return;

            if (!_translations.TryGetValue(lang, out var map))
            {
                map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _translations[lang] = map;
            }

            foreach (var prop in obj.Properties())
            {
                // Skip metadata keys (prefixed with $)
                if (prop.Name.StartsWith("$", StringComparison.Ordinal))
                    continue;

                if (prop.Value.Type == JTokenType.String)
                    map[prop.Name] = prop.Value.Value<string>();
            }

            Debug.Log($"[Localization] Loaded {map.Count} keys for locale '{lang}' from {Path.GetFileName(filePath)}");
        }

        /// <summary>
        /// Try to look up a translated string for the given locale and key.
        /// Returns <c>true</c> if a translation was found.
        /// </summary>
        public bool TryGet(string locale, string key, out string value)
        {
            value = null;
            return !string.IsNullOrEmpty(locale)
                && _translations.TryGetValue(locale, out var map)
                && map.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns all translation entries for a given locale, or an empty dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, string> GetLocale(string locale)
        {
            if (_translations.TryGetValue(locale, out var map))
                return map;
            return new Dictionary<string, string>();
        }

        /// <summary>Drop all loaded translations. Call before a full mod reload.</summary>
        public void Clear() => _translations.Clear();
    }
}
