using UnityEngine;

namespace JG.CursorSystem
{
    /// <summary>
    /// Scene-level helper that raises a preset change as soon as the scene is loaded.
    /// Useful to swap between menu cursors and gameplay crosshairs without extra code.
    /// </summary>
    public sealed class CursorScenePresetSelector : MonoBehaviour
    {
        [SerializeField] string cursorSetId = "Default";
        [SerializeField] string presetId = "Default";
        [SerializeField] bool allowFallbackToDefault = true;
        [SerializeField] bool forceRefresh = true;
        [SerializeField] bool applyOnEnable = true;
        [SerializeField] bool applyOnSceneActive = true;

        [Header("Optional Overrides")]
        [SerializeField] bool overrideCursorVisibility;
        [SerializeField] bool cursorVisible = true;
        [SerializeField] bool overrideLockMode;
        [SerializeField] CursorLockMode lockMode = CursorLockMode.None;

        void OnEnable()
        {
            if (applyOnSceneActive)
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;

            if (applyOnEnable)
                RaiseRequestDeferred();
        }

        void OnDisable()
        {
            if (applyOnSceneActive)
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        void RaiseRequest()
        {
            EventBus<CursorChangeRequestEvent>.Raise(
                new CursorChangeRequestEvent(
                    presetId,
                    string.IsNullOrWhiteSpace(cursorSetId) ? null : cursorSetId,
                    allowFallbackToDefault,
                    overrideCursorVisibility ? cursorVisible : (bool?)null,
                    overrideLockMode ? lockMode : (CursorLockMode?)null,
                    forceRefresh));
        }

        void RaiseRequestDeferred()
        {
            StartCoroutine(RaiseNextFrame());
        }

        System.Collections.IEnumerator RaiseNextFrame()
        {
            yield return null;
            RaiseRequest();
        }

        void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene oldScene, UnityEngine.SceneManagement.Scene newScene)
        {
            if (newScene == gameObject.scene)
                RaiseRequestDeferred();
        }
    }
}
