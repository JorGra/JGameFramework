// ThemeManager.cs  (only defaultTheme serialisation changed)
using JG.Tools;
using UnityEngine;

namespace UI.Theming
{
    /// <summary>Singleton entry-point that stores the active theme.</summary>
    public sealed class ThemeManager : Singleton<ThemeManager>
    {
        [SerializeField] private ThemeAsset defaultTheme;

        public ThemeAsset CurrentTheme { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            CurrentTheme = defaultTheme;
        }

        void Start() => BroadcastTheme();

        public void SetTheme(ThemeAsset newTheme)
        {
            if (newTheme == null || newTheme == CurrentTheme) return;
            CurrentTheme = newTheme;
            BroadcastTheme();
        }

        void BroadcastTheme() =>
            EventBus<ThemeChangedEvent>.Raise(new ThemeChangedEvent(CurrentTheme));
    }

    /// <summary>Raised whenever <see cref="ThemeManager"/> swaps theme.</summary>
    public readonly struct ThemeChangedEvent : IEvent
    {
        public ThemeAsset Theme { get; }
        public ThemeChangedEvent(ThemeAsset theme) => Theme = theme;
    }
}
