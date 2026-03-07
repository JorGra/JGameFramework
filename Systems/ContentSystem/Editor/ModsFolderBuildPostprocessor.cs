using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JG.Modding.Editor
{
    /// <summary>
    /// Copies the Mods folder from the project root into the build output
    /// so that the game can discover mods (especially Core) at runtime.
    /// </summary>
    public class ModsFolderBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var sourceModsDir = Path.Combine(projectRoot, "Mods");

            if (!Directory.Exists(sourceModsDir))
            {
                Debug.LogWarning("[ModsBuildPost] No Mods folder found at project root, skipping copy.");
                return;
            }

            // Build output is e.g. Builds/Win64/Game.exe — we want Builds/Win64/Mods/
            var buildDir = Path.GetDirectoryName(report.summary.outputPath)!;
            var destModsDir = Path.Combine(buildDir, "Mods");

            Debug.Log($"[ModsBuildPost] Copying Mods folder to build: {destModsDir}");
            CopyDirectoryRecursive(sourceModsDir, destModsDir);
            Debug.Log("[ModsBuildPost] Mods folder copied successfully.");
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
