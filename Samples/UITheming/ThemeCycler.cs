using UnityEngine;

namespace UI.Theming.DebugTools
{
    /// <summary>
    /// Development helper that switches through a predefined list of <see cref="ThemeAsset"/>s
    /// whenever a key is pressed. Uses <see cref="ThemeManager.SetTheme"/> to apply changes.
    /// </summary>
    public class ThemeCycler : MonoBehaviour
    {
        [Header("Theme List")]
        [Tooltip("Drag the ThemeAsset instances to test in the desired order.")]
        [SerializeField] private ThemeAsset[] themes = null;

        [Header("Input")]
        [Tooltip("Key that triggers the theme change.")]
        [SerializeField] private KeyCode cycleKey = KeyCode.F2;

        private int currentIndex = -1;

        // ---------------------------------------------------------------------

        void Update()
        {
            if (themes == null || themes.Length == 0) { return; }
            if (!Input.GetKeyDown(cycleKey)) { return; }

            // Advance index and loop.
            currentIndex = (currentIndex + 1) % themes.Length;

            ThemeAsset nextTheme = themes[currentIndex];
            ThemeManager.Instance.SetTheme(nextTheme);

#if UNITY_EDITOR
            Debug.Log($"[ThemeCycler] Switched to theme '{nextTheme.name}' (index {currentIndex}).");
#endif
        }
    }
}
