using System;
using System.Collections;
using UnityEngine;


namespace JG.Tools.SceneManagement
{
    public class SceneLoader : Singleton<SceneLoader>
    {

        [SerializeField] LoadingIndicator loadingIndicator;
        [SerializeField] ScreenFadeController screenFadeController;
        [SerializeField] float minLoadingScreenTime = 1f;

        [Header("Scene Groups")]
        [SerializeField] SceneGroup[] sceneGroups;

        public readonly SceneGroupManager sceneGroupManagement = new SceneGroupManager();
        
        float targetProgress;
        
        public bool IsLoading { get; private set; } = false;

        // Start is called before the first frame update
        IEnumerator Start()
        {
            //screenFadeController = Camera.main.GetComponent<ScreenFadeController>();
            //loadingIndicator = Camera.main.GetComponentInChildren<LoadingIndicator>(true);

            yield return LoadSceneGroupAsync(0, false);
        }


        public void LoadSceneGroup(string name, bool fadeIn = true, bool fadeOut = true)
        {
            StartCoroutine(LoadSceneGroupAsync(name, fadeIn, fadeOut));
        }

        public IEnumerator LoadSceneGroupAsync(string name, bool fadeIn = true, bool fadeOut = true)
        {
            int index = Array.FindIndex(sceneGroups, group => group.GroupName == name);

            if (index == -1)
            {
                Logger.LogError("Scene group not found.");
                yield break;
            }


            yield return LoadSceneGroupAsync(index, fadeIn, fadeOut);
        }

        public IEnumerator LoadSceneGroupAsync(int index, bool fadeIn = true, bool fadeOut = true)
        {
            if(index < 0 || index >= sceneGroups.Length)
            {
                Logger.LogError("Scene group index out of range.");
                yield break;
            }
            targetProgress = 0;
            
            
            if (fadeIn)
            {
                screenFadeController.FadeIn();
                yield return new WaitForSeconds(screenFadeController.defaultFadeDuration);
            }
            EnableLoadingBar(true);
            yield return sceneGroupManagement.UnloadUnneededScenesCoroutine(sceneGroups[index]);
            
            LoadingProgress progress = new LoadingProgress();
            progress.OnProgress += target => targetProgress = Mathf.Max(target, targetProgress);

            yield return sceneGroupManagement.LoadScenesCoroutine(sceneGroups[index], progress);
            yield return new WaitForSeconds(minLoadingScreenTime);


            if (fadeOut)
            {
                EnableLoadingBar(false);
                screenFadeController.FadeOut();

            }

        }

        private void EnableLoadingBar(bool enable)
        {
            IsLoading = enable;

            if (loadingIndicator != null)
            {
                loadingIndicator.ToggleLoadingScreen(enable);
                loadingIndicator.SetLoadingIndicator(targetProgress);
            }
            else
            {
                loadingIndicator = Camera.main.GetComponentInChildren<LoadingIndicator>(true);
                Logger.LogWarning("Loading Indicator not found.");
            }
        }


        public bool GenerationSceneLoaded()
        {
            return sceneGroupManagement.ActiveSceneGroup.GroupName == "GenerationScene";
        }
    }
}