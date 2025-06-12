using System;
using System.Collections;
using UnityEngine;


namespace JG.Tools.SceneManagement
{
    /// <summary>
    /// Loads and unloads <see cref="SceneGroup"/>s, manages a loading screen,
    /// and coordinates screen-fades by emitting <see cref="FadeRequestEvent"/>.
    /// </summary>
    public class SceneLoader : Singleton<SceneLoader>
    {

        [Header("UI")]
        [SerializeField] private LoadingIndicator loadingIndicator;

        [Header("Scene Groups")]
        [SerializeField] private SceneGroup[] sceneGroups;

        [Header("Fade Settings")]
        [Tooltip("Seconds used as duration for all fade requests.")]
        [SerializeField] private float fadeDuration = 1f;

        [Tooltip("Ensures loading screen is visible for at least this time.")]
        [SerializeField] private float minLoadingScreenTime = 1f;

        public readonly SceneGroupManager sceneGroupManagement = new SceneGroupManager();

        private float targetProgress;
        public bool IsLoading { get; private set; }


        private IEnumerator Start()
        {
            // Load first group (index 0) without fades on startup
            yield return LoadSceneGroupAsync(0, fadeIn: false, fadeOut: true);
        }


        /// <summary>Begin loading a <see cref="SceneGroup"/> by name.</summary>
        public void LoadSceneGroup(string groupName,
                                   bool fadeIn = true,
                                   bool fadeOut = true) =>
            StartCoroutine(LoadSceneGroupAsync(groupName, fadeIn, fadeOut));

        /// <summary>Coroutine variant—load group by name.</summary>
        public IEnumerator LoadSceneGroupAsync(string groupName,
                                               bool fadeIn = true,
                                               bool fadeOut = true)
        {
            int index = Array.FindIndex(sceneGroups, g => g.GroupName == groupName);
            if (index == -1)
            {
                Logger.LogError($"Scene group \"{groupName}\" not found.");
                yield break;
            }

            yield return LoadSceneGroupAsync(index, fadeIn, fadeOut);
        }

        /// <summary>Coroutine variant—load group by index.</summary>
        public IEnumerator LoadSceneGroupAsync(int index,
                                               bool fadeIn = true,
                                               bool fadeOut = true)
        {
            if (index < 0 || index >= sceneGroups.Length)
            {
                Logger.LogError("Scene group index out of range.");
                yield break;
            }

            /* ── Fade-in (opaque → clear) before unloading ───────── */
            if (fadeIn)
            {
                RequestFade(fadeIn: true);
                yield return new WaitForSeconds(fadeDuration);
            }

            /* ── Loading workflow ───────────────────────────────── */
            targetProgress = 0f;
            EnableLoadingBar(true);

            // Unload everything not needed by the target group
            yield return sceneGroupManagement.UnloadUnneededScenesCoroutine(sceneGroups[index]);

            // Track progress for loading indicator
            LoadingProgress progress = new LoadingProgress();
            progress.OnProgress += p => targetProgress = Mathf.Max(p, targetProgress);

            // Load all scenes in the group
            yield return sceneGroupManagement.LoadScenesCoroutine(sceneGroups[index], progress);

            // Maintain loading screen for a minimum time
            yield return new WaitForSeconds(minLoadingScreenTime);

            EnableLoadingBar(false);

            /* ── Fade-out (clear → opaque) after load completes ─── */
            if (fadeOut) RequestFade(fadeIn: false);
        }

        private void EnableLoadingBar(bool enable)
        {
            IsLoading = enable;

            // Lazy lookup if reference lost
            if (loadingIndicator == null)
            {
                loadingIndicator = Camera.main?.GetComponentInChildren<LoadingIndicator>(true);
                if (loadingIndicator == null)
                {
                    Logger.LogWarning("LoadingIndicator not found in camera hierarchy.");
                    return;
                }
            }

            loadingIndicator.ToggleLoadingScreen(enable);
            loadingIndicator.SetLoadingIndicator(targetProgress);
        }

        /// <summary>True if the active group is named "GenerationScene".</summary>
        public bool GenerationSceneLoaded() =>
            sceneGroupManagement.ActiveSceneGroup.GroupName == "GenerationScene";

        private void RequestFade(bool fadeIn)
        {
            EventBus<FadeRequestEvent>.Raise(
                new FadeRequestEvent(fadeIn: fadeIn,
                                     duration: fadeDuration,
                                     colorOverride: null,
                                     forceReset: true));   // Always cancel any ongoing fade
        }
    }
}
