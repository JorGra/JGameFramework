using JG.Vfx.Preview;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JG.Vfx.Editor
{
    public static class VfxPreviewSceneCreator
    {
        [MenuItem("Tools/Modding/Vfx/Create Preview Scene")]
        public static void CreatePreviewScene()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Vfx Preview Scene", "VfxPreviewScene", "unity",
                "Choose where to save the preview scene.");
            if (string.IsNullOrEmpty(path))
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 1f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 1f);
            camGo.AddComponent<VfxPreviewCamera>();

            // Lit particle shaders (e.g. URP Particles/Simple Lit) render black without a light.
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var previewGo = new GameObject(VfxPreviewBootstrap.GameObjectName);
            previewGo.AddComponent<VfxPreviewBootstrap>();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Vfx] Preview scene created at {path}. " +
                      "Open it and use Tools > Modding > Vfx > Build Preview (WebGL) to build.");
        }
    }
}
