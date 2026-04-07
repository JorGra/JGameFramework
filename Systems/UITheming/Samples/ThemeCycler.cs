using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.Theming.DebugTools
{
    /// <summary>
    /// Development helper that switches through a predefined list of ThemeAssets
    /// whenever a key is pressed. Uses ThemeManager.SetTheme to apply changes.
    /// </summary>
    public class ThemeCycler : MonoBehaviour
    {
        [Header("Theme List")]
        [Tooltip("Drag the ThemeAsset instances to test in the desired order.")]
        [SerializeField] private ThemeAsset[] themes = null;

        [Header("Input")]
        [Tooltip("Key that triggers the theme change.")]
        [SerializeField] private Key cycleKey = Key.F2;

        private int currentIndex = -1;

        void Update()
        {
            if (themes == null || themes.Length == 0)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (cycleKey == Key.None)
                return;

            if (!keyboard[cycleKey].wasPressedThisFrame)
                return;

            currentIndex = (currentIndex + 1) % themes.Length;

            ThemeAsset nextTheme = themes[currentIndex];
            ThemeManager.Instance.SetTheme(nextTheme);

#if UNITY_EDITOR
            Debug.Log($"[ThemeCycler] Switched to theme '{nextTheme.name}' (index {currentIndex}).");
#endif
        }
    }
}