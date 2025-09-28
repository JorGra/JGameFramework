#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using JG.GameContent;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace JG.GameContent.SchemaExport
{
    internal static class ContentSchemaExporter
    {
        private const string MenuPath = "Tools/Modding/Export Content Schema Snapshot";
        internal static readonly string[] DefaultOutputRelativeSegments =
        {
            "ModCreator",
            "ModCreator",
            "svelte",
            "public",
            "schemas",
            "schema-index.json"
        };

        [MenuItem(MenuPath, priority = 500)]
        public static void Export()
        {
            try
            {
                var defs = DiscoverContentDefs();
                if (defs.Count == 0)
                {
                    EditorUtility.DisplayDialog("Content Schema Export", "No ContentDef types with [ContentFolder] were found.", "OK");
                    return;
                }

                var outputPath = ResolveOutputPath();
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (string.IsNullOrWhiteSpace(outputDirectory))
                    outputDirectory = Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();

                Directory.CreateDirectory(outputDirectory);
                var schemaDirectory = Path.Combine(outputDirectory, "defs");
                Directory.CreateDirectory(schemaDirectory);

                var builder = new JsonContentSchemaBuilder();
                var index = new SchemaIndex
                {
                    exportedAtUtc = DateTime.UtcNow.ToString("o"),
                    schemaVersion = 1
                };

                foreach (var def in defs)
                {
                    var schema = builder.Build(def.Type, def.ContentFolder);
                    var relativeSchemaPath = BuildSchemaRelativePath(def);
                    var absoluteSchemaPath = Path.Combine(outputDirectory, relativeSchemaPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(absoluteSchemaPath) ?? schemaDirectory);

                    File.WriteAllText(absoluteSchemaPath, schema.ToString(Formatting.Indented));

                    index.definitions.Add(new SchemaIndexEntry
                    {
                        typeName = def.Type.FullName,
                        assemblyQualifiedName = def.Type.AssemblyQualifiedName,
                        contentFolder = def.ContentFolder,
                        schemaFile = NormalizePathSeparators(relativeSchemaPath),
                        displayName = ObjectNames.NicifyVariableName(def.Type.Name)
                    });
                }

                var indexJson = JsonConvert.SerializeObject(index, Formatting.Indented);
                File.WriteAllText(outputPath, indexJson);

                Debug.Log($"[ContentSchemaExporter] Exported {index.definitions.Count} schemas to {outputDirectory}.");
                EditorUtility.DisplayDialog(
                    "Content Schema Export",
                    $"Exported {index.definitions.Count} content schemas to:\n{outputDirectory}",
                    "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContentSchemaExporter] Export failed: {ex}");
                EditorUtility.DisplayDialog("Content Schema Export", "Export failed. See console for details.", "OK");
            }
        }

        private static List<ContentDefInfo> DiscoverContentDefs()
        {
            var results = new List<ContentDefInfo>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                if (assembly == null || assembly.IsDynamic)
                    continue;

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException rtlEx)
                {
                    types = rtlEx.Types;
                    if (rtlEx.LoaderExceptions != null)
                    {
                        foreach (var loaderEx in rtlEx.LoaderExceptions)
                        {
                            Debug.LogWarning($"[ContentSchemaExporter] Failed to load type from {assembly.FullName}: {loaderEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ContentSchemaExporter] Skipping assembly {assembly.FullName}: {ex.Message}");
                    continue;
                }

                if (types == null)
                    continue;

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract)
                        continue;

                    if (!typeof(IContentDef).IsAssignableFrom(type))
                        continue;

                    var folderAttribute = type.GetCustomAttribute<ContentFolderAttribute>();
                    if (folderAttribute == null)
                        continue;

                    results.Add(new ContentDefInfo(type, folderAttribute.FolderName));
                }
            }

            return results
                .OrderBy(r => r.ContentFolder, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Type.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ResolveOutputPath()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();
            var settings = ContentSchemaExporterSettings.LoadOrCreate();
            return settings.ResolveOutputPath(projectRoot, DefaultOutputRelativeSegments);
        }

        private static string BuildSchemaRelativePath(ContentDefInfo def)
        {
            var folderSegment = SanitizeSegment(string.IsNullOrWhiteSpace(def.ContentFolder) ? "content" : def.ContentFolder.Replace('/', '.'));
            var typeSegment = SanitizeSegment(def.Type.Name);
            var fileName = $"{folderSegment}.{typeSegment}.schema.json";
            return Path.Combine("defs", fileName);
        }

        private static string SanitizeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "content";

            var sb = new StringBuilder();
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(char.ToLowerInvariant(ch));
                else if (ch == '.' || ch == '-' || ch == '_')
                    sb.Append(ch);
                else
                    sb.Append('-');
            }
            var result = sb.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(result) ? "content" : result;
        }

        private static string NormalizePathSeparators(string value)
        {
            return value.Replace("\\", "/");
        }

        private sealed class ContentDefInfo
        {
            public ContentDefInfo(Type type, string folder)
            {
                Type = type;
                ContentFolder = folder;
            }

            public Type Type { get; }
            public string ContentFolder { get; }
        }

        private sealed class SchemaIndex
        {
            public int schemaVersion;
            public string exportedAtUtc;
            public List<SchemaIndexEntry> definitions = new();
        }

        private sealed class SchemaIndexEntry
        {
            public string typeName;
            public string assemblyQualifiedName;
            public string contentFolder;
            public string schemaFile;
            public string displayName;
        }
    }
}
#endif
