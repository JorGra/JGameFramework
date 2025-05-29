using UnityEngine;
using JG.Tools;

namespace UI.Theming
{
    /// <summary>
    /// Central point for reading and changing the active UI theme.
    /// </summary>
    public class ThemeManager : Singleton<ThemeManager>
    {
        [SerializeField] private ThemeAsset defaultTheme;

        /// <summary>The currently applied global theme.</summary>
        public ThemeAsset CurrentTheme { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (defaultTheme == null)
            {
                Debug.LogError($"{nameof(ThemeManager)}: Default theme missing.");
            }
            CurrentTheme = defaultTheme;
        }

        void Start()
        {
            BroadcastTheme();
        }

        /// <summary>
        /// Set a new global theme and notify all listeners.
        /// </summary>
        public void SetTheme(ThemeAsset newTheme)
        {
            if (newTheme == null || newTheme == CurrentTheme) return;

            CurrentTheme = newTheme;
            BroadcastTheme();
        }

        void BroadcastTheme()
        {
            EventBus<ThemeChangedEvent>.Raise(new ThemeChangedEvent(CurrentTheme));
        }
    }

    /// <summary>Raised globally whenever the active theme changes.</summary>
    public struct ThemeChangedEvent : IEvent
    {
        public ThemeAsset Theme { get; }

        public ThemeChangedEvent(ThemeAsset theme) => Theme = theme;
    }
}
