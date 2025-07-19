using System.Collections;
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

        //#if !Unity_6
        //        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        //        static IEnumerator Init()
        //        {
        //            Logger.Log("Bootstrapper Init...");
        //            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

        //#if UNITY_EDITOR
        //            // Set the bootstrapper scene to be the play mode start scene when running in the editor
        //            // This will cause the bootstrapper scene to be loaded first (and only once) when entering
        //            // play mode from the Unity Editor, regardless of which scene is currently active.
        //            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
        //#endif
        //        }
        //#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()          // <- static, void, no params
        {
            Debug.Log("Bootstrapper Init...");

            // If you really need it to be async you can still use
            // LoadSceneAsync, just don’t try to *yield* here.
            var op = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            // optional: hook a callback instead of yielding
            // op.completed += _ => { /* do something after scene loaded */ };
        }

#if UNITY_EDITOR
        // --- Editor: runs when scripts reload (incl. before you hit Play) ---
        [InitializeOnLoadMethod]            // Editor-time attribute
        private static void SetStartScene()
        {
            var firstSceneAsset = AssetDatabase
                                   .LoadAssetAtPath<SceneAsset>(
                                        EditorBuildSettings.scenes[0].path);
            EditorSceneManager.playModeStartScene = firstSceneAsset;
        }
#endif
        //#endif
    }
}