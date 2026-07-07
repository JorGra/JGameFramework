using UnityEngine;

namespace JG.CursorSystem
{
    /// <summary>
    /// Scene-level helper that holds a <see cref="CursorLayer.Scene"/> claim while enabled.
    /// Useful to swap between menu cursors and gameplay crosshairs without extra code:
    /// the claim is released automatically when the scene unloads, revealing whatever lies beneath.
    /// </summary>
    [AddComponentMenu("JGameFramework/Cursor System/Cursor Scene Preset Selector")]
    public sealed class CursorScenePresetSelector : MonoBehaviour
    {
        [SerializeField] string cursorSetId = "Default";
        [SerializeField] string presetId = "Default";
        [SerializeField] bool allowFallbackToDefault = true;
        [Tooltip("Added to the Scene layer priority; lets one selector outrank another (e.g. sub-menu over menu).")]
        [SerializeField] int priorityOffset = 0;

        [Header("Optional Overrides")]
        [SerializeField] bool overrideCursorVisibility;
        [SerializeField] bool cursorVisible = true;
        [SerializeField] bool overrideLockMode;
        [SerializeField] CursorLockMode lockMode = CursorLockMode.None;

        CursorClaimHandle claim;

        void OnEnable()
        {
            claim = CursorClaimStack.Push(
                presetId,
                string.IsNullOrWhiteSpace(cursorSetId) ? null : cursorSetId,
                CursorLayer.Scene + priorityOffset,
                owner: this,
                allowFallback: allowFallbackToDefault,
                visibility: overrideCursorVisibility ? cursorVisible : (bool?)null,
                lockMode: overrideLockMode ? lockMode : (CursorLockMode?)null);
        }

        void OnDisable()
        {
            claim?.Dispose();
            claim = null;
        }
    }
}
