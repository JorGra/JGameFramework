#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JG.GameContent.SchemaExport
{
    /// <summary>
    /// Copies the exported schemas folder into the build output so that
    /// external tools (e.g. the Mod Creator web UI) can discover them
    /// when pointed at the game directory.
    /// </summary>
    public class SchemaFolderBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1;

        public void OnPostprocessBuild(BuildReport report)
        {
            var settings = ContentSchemaExporterSettings.LoadOrCreate();
            var projectRoot = Path.GetDirectoryName(Application.dataPath)
                              ?? Directory.GetCurrentDirectory();
            var outputPath = settings.ResolveOutputPath(
                projectRoot, ContentSchemaExporter.DefaultOutputRelativeSegments);
            var schemasSourceDir = Path.GetDirectoryName(outputPath);

            if (string.IsNullOrWhiteSpace(schemasSourceDir))
            {
                Debug.LogWarning("[SchemasBuildPost] Could not resolve schema output directory, skipping copy.");
                return;
            }

            if (!Directory.Exists(schemasSourceDir))
            {
                Debug.LogWarning(
                    $"[SchemasBuildPost] Schema directory not found at {schemasSourceDir}, skipping copy. " +
                    "Run Tools > Modding > Export Content Schema Snapshot first.");
                return;
            }

            var buildDir = Path.GetDirectoryName(report.summary.outputPath)!;
            var destSchemasDir = Path.Combine(buildDir, "schemas");

            if (string.Equals(
                    Path.GetFullPath(schemasSourceDir),
                    Path.GetFullPath(destSchemasDir),
                    StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[SchemasBuildPost] Source and destination are the same path, skipping copy.");
                return;
            }

            Debug.Log($"[SchemasBuildPost] Copying schemas folder to build: {destSchemasDir}");
            CopyDirectoryRecursive(schemasSourceDir, destSchemasDir);
            Debug.Log("[SchemasBuildPost] Schemas folder copied successfully.");
        }

        static void CopyDirectoryRecursive(string source, string destination)
        {
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);

            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source))
            {
                var destFile = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                var destDir = Path.Combine(destination, Path.GetFileName(dir));
                CopyDirectoryRecursive(dir, destDir);
            }
        }
    }
}
#endif
