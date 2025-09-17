#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JG.Tools.SceneManagement.Editor
{
    public static class BootstrapperEditorUtility
    {
        private const string PrefKey = "QuickSceneSwitcher.BootstrapperEnabled";

        public static bool IsBootstrapperEnabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        public static void ApplyBootstrapperSceneSetting()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            if (IsBootstrapperEnabled)
            {
                var firstScenePath = scenes[0].path;
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(firstScenePath);
                EditorSceneManager.playModeStartScene = sceneAsset;
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
        }
    }
}
#endif

