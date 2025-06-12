using JG.Audio;
using UnityEngine;

/// <summary>
/// Listens for <see cref="PlayUISoundEvent"/> and forwards the correct
/// <see cref="PlaySoundEvent"/> to the global sound system based on
/// the active <see cref="UIAudioTheme"/>.
/// </summary>
[DisallowMultipleComponent]
public class UIAudioThemePlayer : MonoBehaviour
{
    [Tooltip("Theme containing every UI-audio profile.")]
    [SerializeField] private UIAudioTheme theme;

    EventBinding<PlayUISoundEvent> binding;

    void OnEnable()
    {
        binding = new EventBinding<PlayUISoundEvent>(OnPlayUISound);
        EventBus<PlayUISoundEvent>.Register(binding);
    }

    void OnDisable()
    {
        EventBus<PlayUISoundEvent>.Deregister(binding);
    }

    /// <summary>Handles UI sound-play requests.</summary>
    /// <param name="e">Event issued by a UI control.</param>
    void OnPlayUISound(PlayUISoundEvent e)
    {
        if (theme == null) return;

        if (theme.TryGetSound(e.Profile, e.Action, out var snd))
        {
            EventBus<PlaySoundEvent>.Raise(
                new PlaySoundEvent(
                    snd.SoundData,
                    e.Position,
                    snd.RandomPitch,
                    snd.RandomPitchRange));
        }
    }
}
/// <summary>Logical UI actions that can trigger audio.</summary>
public enum UIAudioAction
{
    Hover,
    Press,
    Click,
    Release
}

/// <summary>
/// Raised by UI controls to request a UI sound based on <c>profile + action</c>.
/// The audio theme player converts this into a concrete <see cref="PlaySoundEvent"/>.
/// </summary>
public readonly struct PlayUISoundEvent : IEvent
{
    public string Profile { get; }
    public UIAudioAction Action { get; }
    public Vector3 Position { get; }

    public PlayUISoundEvent(string profile, UIAudioAction action, Vector3 position)
    {
        Profile = profile;
        Action = action;
        Position = position;
    }
}
