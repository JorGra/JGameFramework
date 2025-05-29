using UnityEngine;

namespace UI.Theming
{
    /// <summary>
    /// Attach to a Canvas or panel root to override the global theme for its children.
    /// Works by applying the chosen theme immediately to all <see cref="IThemeable"/> components
    /// below this transform.
    /// </summary>
    public class ThemeOverride : MonoBehaviour
    {
        [SerializeField] private ThemeAsset overrideTheme;

        void OnEnable()
        {
            if (overrideTheme == null) return;

            var themables = GetComponentsInChildren<IThemeable>(true);
            foreach (var t in themables)
            {
                t.ApplyTheme(overrideTheme);
            }
        }
    }
}
