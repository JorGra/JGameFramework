using System;
using System.IO;
using System.Linq;
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
            // WebGL's output path is the build folder itself — we want Mods/ beside index.html.
            bool isWebGL = report.summary.platform == BuildTarget.WebGL;
            var buildDir = isWebGL
                ? report.summary.outputPath
                : Path.GetDirectoryName(report.summary.outputPath)!;
            var destModsDir = Path.Combine(buildDir, "Mods");

            Debug.Log($"[ModsBuildPost] Copying Mods folder to build: {destModsDir}");
            CopyDirectoryRecursive(sourceModsDir, destModsDir);
            Debug.Log("[ModsBuildPost] Mods folder copied successfully.");

            if (isWebGL)
                WriteWebIndex(destModsDir);
        }

        /// <summary>
        /// HTTP has no directory listing, so ModsVfsPreload.jspre needs a list of every file
        /// to fetch into the browser's in-memory filesystem before the engine starts.
        /// </summary>
        static void WriteWebIndex(string modsDir)
        {
            var files = Directory.GetFiles(modsDir, "*", SearchOption.AllDirectories)
                .Where(f => !IsExcludedFromWeb(f))
                .Select(f => Path.GetRelativePath(modsDir, f).Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

            var index = new WebIndex { version = DateTime.UtcNow.Ticks.ToString(), files = files };
            File.WriteAllText(Path.Combine(modsDir, WebIndexFileName), JsonUtility.ToJson(index));
            Debug.Log($"[ModsBuildPost] Wrote {WebIndexFileName} with {files.Length} files.");
        }

        static bool IsExcludedFromWeb(string file)
        {
            var name = Path.GetFileName(file);
            if (name == WebIndexFileName || name == ".gitkeep")
                return true;

            return WebExcludedExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase);
        }

        const string WebIndexFileName = "mods-index.json";

        // Code mods cannot load under IL2CPP; the rest is never read at runtime.
        static readonly string[] WebExcludedExtensions = { ".dll", ".pdb", ".bak", ".meta", ".md" };

        [Serializable]
        class WebIndex
        {
            public string version;
            public string[] files;
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
