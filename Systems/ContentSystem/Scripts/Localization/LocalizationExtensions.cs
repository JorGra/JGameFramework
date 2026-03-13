using UnityEngine;

namespace JG.GameContent.Localization
{
    /// <summary>
    /// Extension helpers for looking up translated content strings at runtime.
    /// Falls back to the English source text when no translation exists.
    /// </summary>
    public static class LocalizationExtensions
    {
        private static string _currentLocaleCode;

        /// <summary>
        /// The active locale code used for translation lookups (e.g. "en", "de").
        /// Defaults to the system language. Set this to change the active language.
        /// </summary>
        public static string CurrentLocaleCode
        {
            get
            {
                if (_currentLocaleCode == null)
                    _currentLocaleCode = SystemLanguageToCode(Application.systemLanguage);
                return _currentLocaleCode;
            }
            set => _currentLocaleCode = value;
        }

        /// <summary>
        /// Returns the translated value for <paramref name="fieldName"/> on this
        /// content definition, or <paramref name="fallback"/> if no translation is
        /// available for the current locale.
        /// </summary>
        /// <example>
        /// <code>Title = this.GetTranslated(nameof(DisplayName), DisplayName);</code>
        /// </example>
        public static string GetTranslated(this IContentDef def, string fieldName, string fallback)
        {
            if (def == null || string.IsNullOrEmpty(def.Id))
                return fallback;

            var locale = CurrentLocaleCode;
            if (string.IsNullOrEmpty(locale))
                return fallback;

            var key = $"{def.Id}.{fieldName}";
            if (ModTranslationLoader.Instance.TryGet(locale, key, out var translated))
                return translated;

            return fallback;
        }

        static string SystemLanguageToCode(SystemLanguage lang)
        {
            return lang switch
            {
                SystemLanguage.English => "en",
                SystemLanguage.German => "de",
                SystemLanguage.French => "fr",
                SystemLanguage.Spanish => "es",
                SystemLanguage.Italian => "it",
                SystemLanguage.Portuguese => "pt",
                SystemLanguage.Russian => "ru",
                SystemLanguage.Chinese => "zh",
                SystemLanguage.Japanese => "ja",
                SystemLanguage.Korean => "ko",
                SystemLanguage.Dutch => "nl",
                SystemLanguage.Polish => "pl",
                SystemLanguage.Turkish => "tr",
                SystemLanguage.Swedish => "sv",
                SystemLanguage.Norwegian => "no",
                SystemLanguage.Danish => "da",
                SystemLanguage.Finnish => "fi",
                SystemLanguage.Czech => "cs",
                SystemLanguage.Hungarian => "hu",
                SystemLanguage.Romanian => "ro",
                SystemLanguage.Thai => "th",
                SystemLanguage.Vietnamese => "vi",
                SystemLanguage.Indonesian => "id",
                SystemLanguage.Ukrainian => "uk",
                SystemLanguage.Arabic => "ar",
                SystemLanguage.Hebrew => "he",
                _ => "en"
            };
        }
    }
}
