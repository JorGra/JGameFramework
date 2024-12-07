using System.Collections;
using System.Collections.Generic;
using JG.Tools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JG.Tools.SceneManagement
{
    public class Bootstrapper : Singleton<Bootstrapper>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static IEnumerator Init()
        {
            Logger.Log("Bootstrapper Init...");
            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

#if UNITY_EDITOR
            // Set the bootstrapper scene to be the play mode start scene when running in the editor
            // This will cause the bootstrapper scene to be loaded first (and only once) when entering
            // play mode from the Unity Editor, regardless of which scene is currently active.
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
#endif
        }
    }
}