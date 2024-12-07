using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JG.Tools.SceneManagement
{

    public class SceneGroupManager
    {
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action<string> OnSceneGroupUnloadBegin = delegate { };
        public event Action<string> OnSceneGroupLoaded = delegate { };

        public SceneGroup ActiveSceneGroup { get; private set; }

        public void LoadScenes(SceneGroup group, IProgress<float> progressCallback, bool reloadDupScenes = false)
        {
            CoroutineRunner.StartCoroutine(LoadScenesCoroutine(group, progressCallback, reloadDupScenes));
        }

        public void UnloadScenes()
        {
            CoroutineRunner.StartCoroutine(UnloadScenesCoroutine());
        }


        //TODO : Add a way to load only specific scenes, for example only the ones that are not loaded yet! for minimap for example
        public IEnumerator LoadScenesCoroutine(SceneGroup group, IProgress<float> progress, bool reloadDupScenes = false)
        {
            ActiveSceneGroup = group;

            var loadedScenes = new List<string>();
            int sceneCount = SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var totalScenesToLoad = ActiveSceneGroup.Scenes.Count;

            var operationGroup = new AsyncOperationGroup(totalScenesToLoad);

            for (int i = 0;i < totalScenesToLoad; i++)
            {
                var sceneData = ActiveSceneGroup.Scenes[i];
                if (reloadDupScenes == false && loadedScenes.Contains(sceneData.Name)) continue;

                var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);

                operationGroup.Operations.Add(operation);

                OnSceneLoaded.Invoke(sceneData.Name);
            }

            while (!operationGroup.IsDone)
            {
                progress.Report(operationGroup.Progress);
                yield return null;
            }

            Scene activeScene = SceneManager.GetSceneByName(ActiveSceneGroup.FindSceneNameByType(SceneType.ActiveScene));

            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }

            OnSceneGroupLoaded.Invoke(group.GroupName);
        }

        public IEnumerator UnloadScenesCoroutine()
        {
            OnSceneGroupUnloadBegin.Invoke(ActiveSceneGroup.GroupName);

            var scenes = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;

            int sceneCount = SceneManager.sceneCount;

            for (int i = sceneCount - 1; i > 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var sceneName = scene.name;
                if ( scene.buildIndex == 0) continue;

                scenes.Add(sceneName);
            }


            var operationGroup = new AsyncOperationGroup(scenes.Count);
            foreach (var scene in scenes)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null) continue;


                operationGroup.Operations.Add(operation);

                OnSceneUnloaded.Invoke(scene);
            }

            while (!operationGroup.IsDone)
            {
                yield return null;
            }
        }

        public IEnumerator UnloadUnneededScenesCoroutine(SceneGroup newSceneGroup)
        {
            OnSceneGroupUnloadBegin.Invoke(newSceneGroup.GroupName);

            var currentlyLoadedScenes = new List<string>();
            int sceneCount = SceneManager.sceneCount;

            // Collect names of currently loaded scenes
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.buildIndex != 0) // Exclude scene at index 0 (usually the persistent scene)
                {
                    currentlyLoadedScenes.Add(scene.name);
                }
            }

            // Determine which scenes need to be unloaded
            var scenesToUnload = currentlyLoadedScenes.Where(sceneName =>
                !newSceneGroup.Scenes.Any(sceneData => sceneData.Name == sceneName)).ToList();

            var operationGroup = new AsyncOperationGroup(scenesToUnload.Count);

            foreach (var sceneToUnload in scenesToUnload)
            {
                var operation = SceneManager.UnloadSceneAsync(sceneToUnload);
                if (operation != null)
                {
                    operationGroup.Operations.Add(operation);
                    OnSceneUnloaded.Invoke(sceneToUnload);
                }
            }

            while (!operationGroup.IsDone)
            {
                yield return null;
            }
        }
    }


    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;

        public float Progress => Operations.Count == 0 ? 0 : Operations.Average(o => o.progress);
        public bool IsDone => Operations.All(o => o.isDone);

        public AsyncOperationGroup(int initialCapacity)
        {
            Operations = new List<AsyncOperation>(initialCapacity);
        }
    }

    public class LoadingProgress : IProgress<float>
    {

        public event Action<float> OnProgress;

        const float ratio = 1;
        public void Report(float value)
        {
            OnProgress?.Invoke(value / ratio);
        }
    }
}
