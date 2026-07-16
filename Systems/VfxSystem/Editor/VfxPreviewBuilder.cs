using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JG.Vfx.Editor
{
    public static class VfxPreviewBuilder
    {
        [MenuItem("Tools/Modding/Vfx/Build Preview (WebGL)")]
        public static void BuildPreview()
        {
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                EditorUtility.DisplayDialog("Vfx Preview Build",
                    "Open (and save) the Vfx preview scene first, then run this again. " +
                    "It builds the currently active scene.", "OK");
                return;
            }

            var outputDir = EditorUtility.SaveFolderPanel("Vfx Preview build output",
                Path.GetDirectoryName(Application.dataPath), "VfxPreviewBuild");
            if (string.IsNullOrEmpty(outputDir))
                return;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene.path },
                locationPathName = outputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[Vfx] Preview build succeeded: {outputDir} " +
                          $"({report.summary.totalSize / (1024 * 1024)} MB)");
            else
                Debug.LogError($"[Vfx] Preview build failed: {report.summary.result}");
        }
    }
}
