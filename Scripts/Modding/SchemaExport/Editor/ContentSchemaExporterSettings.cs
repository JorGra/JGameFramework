#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JG.GameContent.SchemaExport
{
    internal sealed class ContentSchemaExporterSettings : ScriptableObject
    {
        private const string AssetPath = "Assets/JGameFramework/Scripts/Modding/SchemaExport/Editor/ContentSchemaExporterSettings.asset";

        [SerializeField] private bool outputPathIsRelative = true;
        [SerializeField] private string outputPath = string.Join("/", ContentSchemaExporter.DefaultOutputRelativeSegments);

        internal static ContentSchemaExporterSettings LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ContentSchemaExporterSettings>(AssetPath);
            if (settings != null)
            {
                if (!string.IsNullOrEmpty(settings.outputPath) && settings.outputPath.EndsWith("schema-index-snapshot.json"))
                {
                    settings.outputPath = string.Join("/", ContentSchemaExporter.DefaultOutputRelativeSegments);
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }
                return settings;
            }

            settings = CreateInstance<ContentSchemaExporterSettings>();
            settings.outputPathIsRelative = true;
            settings.outputPath = string.Join("/", ContentSchemaExporter.DefaultOutputRelativeSegments);
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ContentSchemaExporterSettings] Created settings asset at " + AssetPath);
            return settings;
        }

        internal string ResolveOutputPath(string projectRoot, string[] defaultSegments)
        {
            var raw = string.IsNullOrWhiteSpace(outputPath)
                ? string.Join(Path.DirectorySeparatorChar.ToString(), defaultSegments)
                : outputPath.Trim();

            raw = NormalizeSeparators(raw);

            if (outputPathIsRelative || !Path.IsPathRooted(raw))
                return Path.Combine(projectRoot, raw);

            return raw;
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
    }
}
#endif
