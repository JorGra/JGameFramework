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
        [SerializeField] bool forceRefresh;

        [Header("Optional Overrides")]
        [SerializeField] bool overrideCursorVisibility;
        [SerializeField] bool cursorVisible = true;
        [SerializeField] bool overrideLockMode;
        [SerializeField] CursorLockMode lockMode = CursorLockMode.None;

        void Start()
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
    }
}
