#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JG.GameContent.SchemaExport
{
    internal sealed class ContentSchemaExporterSettings : ScriptableObject
    {
        private const string DefaultCreatePath = "Assets/Settings/ContentSchemaExporterSettings.asset";

        [SerializeField] private bool outputPathIsRelative = true;
        [SerializeField] private string outputPath = string.Join("/", ContentSchemaExporter.DefaultOutputRelativeSegments);

        [Header("Metadata")]
        [SerializeField][Min(1)] private int schemaVersion = 1;
        [SerializeField] private string gameName = string.Empty;
        [SerializeField] private bool useProjectVersion = true;
        [SerializeField] private string gameVersion = string.Empty;
        [SerializeField] private string publisherName = string.Empty;
        [SerializeField][TextArea] private string gameDescription = string.Empty;
        [SerializeField] private Texture2D gameIcon;
        [SerializeField] private string websiteUrl = string.Empty;
        [SerializeField] private string supportUrl = string.Empty;
        [SerializeField] private List<string> tags = new List<string>();
        [SerializeField] private List<AdditionalMetadataEntry> additionalMetadata = new List<AdditionalMetadataEntry>();

        internal static ContentSchemaExporterSettings LoadOrCreate()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(ContentSchemaExporterSettings)}");
            if (guids.Length > 0)
            {
                if (guids.Length > 1)
                    Debug.LogWarning("[ContentSchemaExporterSettings] Multiple settings assets found. Using: "
                                     + AssetDatabase.GUIDToAssetPath(guids[0]));

                var settings = AssetDatabase.LoadAssetAtPath<ContentSchemaExporterSettings>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
                if (settings != null)
                {
                    settings.EnsureCollections();
                    return settings;
                }
            }

            var created = CreateInstance<ContentSchemaExporterSettings>();
            Directory.CreateDirectory(Path.GetDirectoryName(DefaultCreatePath));
            AssetDatabase.CreateAsset(created, DefaultCreatePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ContentSchemaExporterSettings] Created settings asset at " + DefaultCreatePath);
            return created;
        }

        internal string ResolveOutputPath(string projectRoot, string[] defaultSegments)
        {
            var raw = string.IsNullOrWhiteSpace(outputPath)
                ? string.Join(Path.DirectorySeparatorChar.ToString(), defaultSegments)
                : outputPath.Trim();

            raw = NormalizeSeparators(raw);

            // Migrate legacy paths that included the filename.
            if (raw.EndsWith("schema-index.json", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(0, raw.Length - "/schema-index.json".Length);

            string folder;
            if (outputPathIsRelative || !Path.IsPathRooted(raw))
                folder = Path.Combine(projectRoot, raw);
            else
                folder = raw;

            return Path.Combine(folder, "schema-index.json");
        }

        internal int GetSchemaVersion()
        {
            return Mathf.Max(1, schemaVersion);
        }

        internal string GetGameName()
        {
            if (!string.IsNullOrWhiteSpace(gameName))
                return gameName.Trim();

            var defaultName = Application.productName;
            return string.IsNullOrWhiteSpace(defaultName) ? "Game" : defaultName.Trim();
        }

        internal string GetGameVersion()
        {
            if (useProjectVersion)
                return string.IsNullOrWhiteSpace(Application.version) ? "0.0.0" : Application.version.Trim();

            if (!string.IsNullOrWhiteSpace(gameVersion))
                return gameVersion.Trim();

            return "0.0.0";
        }

        internal string GetPublisherName()
        {
            return string.IsNullOrWhiteSpace(publisherName) ? string.Empty : publisherName.Trim();
        }

        internal string GetGameDescription()
        {
            return string.IsNullOrWhiteSpace(gameDescription) ? string.Empty : gameDescription.Trim();
        }

        internal string GetWebsiteUrl()
        {
            return string.IsNullOrWhiteSpace(websiteUrl) ? string.Empty : websiteUrl.Trim();
        }

        internal string GetSupportUrl()
        {
            return string.IsNullOrWhiteSpace(supportUrl) ? string.Empty : supportUrl.Trim();
        }

        internal Texture2D GetGameIcon()
        {
            return gameIcon;
        }

        internal IEnumerable<string> EnumerateTags()
        {
            EnsureCollections();
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                yield return tag.Trim();
            }
        }

        internal IEnumerable<KeyValuePair<string, string>> EnumerateAdditionalMetadata()
        {
            EnsureCollections();
            foreach (var entry in additionalMetadata)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;

                var key = entry.key.Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                var value = entry.value?.Trim() ?? string.Empty;
                yield return new KeyValuePair<string, string>(key, value);
            }
        }

        private void OnEnable()
        {
            EnsureCollections();
        }

        private void OnValidate()
        {
            EnsureCollections();
        }

        private void EnsureCollections()
        {
            if (tags == null)
                tags = new List<string>();

            if (additionalMetadata == null)
                additionalMetadata = new List<AdditionalMetadataEntry>();
        }

        private static string NormalizeSeparators(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            value = value.Replace('\\', Path.DirectorySeparatorChar);
            value = value.Replace('/', Path.DirectorySeparatorChar);
            return value;
        }

        [MenuItem("Tools/Modding/Content Schema Export Settings", priority = 501)]
        private static void SelectSettings()
        {
            var settings = LoadOrCreate();
            Selection.activeObject = settings;
        }

        [Serializable]
        private sealed class AdditionalMetadataEntry
        {
            public string key = string.Empty;
            public string value = string.Empty;
        }
    }
}
#endif
