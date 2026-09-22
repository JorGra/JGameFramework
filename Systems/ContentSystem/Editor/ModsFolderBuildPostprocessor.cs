using System;
using System.Collections.Generic;
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

            if (isWebGL)
            {
                // Web hosts cap file counts (itch.io: 1000) and HTTP has no directory listing,
                // so ship a single pack + index instead of the loose tree.
                Debug.Log($"[ModsBuildPost] Packing Mods folder for web: {destModsDir}");
                WriteWebPack(sourceModsDir, destModsDir);
                return;
            }

            Debug.Log($"[ModsBuildPost] Copying Mods folder to build: {destModsDir}");
            CopyDirectoryRecursive(sourceModsDir, destModsDir);
            Debug.Log("[ModsBuildPost] Mods folder copied successfully.");
        }

        /// <summary>
        /// Writes Mods/mods.pack (all mod files concatenated) and Mods/mods-index.json
        /// (path, offset, size per file). ModsVfsPreload.jspre fetches the pack once and
        /// slices it into the browser's in-memory filesystem before the engine starts.
        /// </summary>
        static void WriteWebPack(string sourceModsDir, string destModsDir)
        {
            if (Directory.Exists(destModsDir))
                Directory.Delete(destModsDir, true);
            Directory.CreateDirectory(destModsDir);

            var files = Directory.GetFiles(sourceModsDir, "*", SearchOption.AllDirectories)
                .Where(f => !IsExcludedFromWeb(f))
                .Select(f => Path.GetRelativePath(sourceModsDir, f).Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

            var entries = new List<WebPackEntry>(files.Length);
            long offset = 0;

            using (var pack = new FileStream(Path.Combine(destModsDir, WebPackFileName), FileMode.Create, FileAccess.Write))
            {
                foreach (var rel in files)
                {
                    using var input = File.OpenRead(Path.Combine(sourceModsDir, rel));
                    input.CopyTo(pack);
                    entries.Add(new WebPackEntry { path = rel, offset = offset, size = input.Length });
                    offset += input.Length;
                }
            }

            var index = new WebIndex
            {
                version = DateTime.UtcNow.Ticks.ToString(),
                pack = WebPackFileName,
                files = files,
                entries = entries.ToArray(),
            };
            File.WriteAllText(Path.Combine(destModsDir, WebIndexFileName), JsonUtility.ToJson(index));
            Debug.Log($"[ModsBuildPost] Wrote {WebPackFileName} ({offset / (1024f * 1024f):F1} MB, {files.Length} files) and {WebIndexFileName}.");
        }

        static bool IsExcludedFromWeb(string file)
        {
            var name = Path.GetFileName(file);
            if (name == WebIndexFileName || name == ".gitkeep")
                return true;

            return WebExcludedExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase);
        }

        const string WebIndexFileName = "mods-index.json";
        const string WebPackFileName = "mods.pack";

        // Code mods cannot load under IL2CPP; the rest is never read at runtime.
        static readonly string[] WebExcludedExtensions = { ".dll", ".pdb", ".bak", ".meta", ".md" };

        [Serializable]
        class WebIndex
        {
            public string version;
            public string pack;
            public string[] files;
            public WebPackEntry[] entries;
        }

        [Serializable]
        class WebPackEntry
        {
            public string path;
            public long offset;
            public long size;
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
