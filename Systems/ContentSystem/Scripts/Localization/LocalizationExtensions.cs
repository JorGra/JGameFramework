using UnityEngine.Localization.Settings;

namespace JG.GameContent.Localization
{
    /// <summary>
    /// Extension helpers for looking up translated content strings at runtime.
    /// Falls back to the English source text when no translation exists.
    /// </summary>
    public static class LocalizationExtensions
    {
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

            var locale = LocalizationSettings.SelectedLocale;
            if (locale == null)
                return fallback;

            var key = $"{def.Id}.{fieldName}";
            if (ModTranslationLoader.Instance.TryGet(locale.Identifier.Code, key, out var translated))
                return translated;

            return fallback;
        }
    }
}
