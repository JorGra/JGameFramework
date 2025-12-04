using System;
using System.Collections.Generic;
using JG.Audio;
using UnityEngine;

/// <summary>
/// ScriptableObject defining every UI-sound profile in a single place.
/// A profile groups sounds for Hover / Press / Click / Release.
/// </summary>
[CreateAssetMenu(fileName = "UIAudioTheme",
                 menuName = "Audio/UI Audio Theme",
                 order = 51)]
public class UIAudioTheme : ScriptableObject
{
    #region Nested Types
    [Serializable]
    public struct ActionSound
    {
        public UIAudioAction Action;
        public SoundData SoundData;
        public bool RandomPitch;
        public Vector2 RandomPitchRange;
    }

    [Serializable]
    public class Profile
    {
        [Tooltip("Profile identifier (e.g. \"Default\", \"PrimaryButton\", \"Danger\").")]
        public string ProfileName = "Default";

        [Tooltip("Sounds played by this profile. 1 entry per action is recommended.")]
        public List<ActionSound> Sounds = new();
    }
    #endregion

    [SerializeField] private List<Profile> profiles = new();

    // Lazily-rebuilt lookup for O(1) access at runtime.
    Dictionary<string, Dictionary<UIAudioAction, ActionSound>> lookup;

    void OnEnable() => BuildLookup();

    /// <summary>
    /// Attempts to fetch a sound for the requested profile + action.
    /// Falls back to the <c>"Default"</c> profile if not found.
    /// </summary>
    public bool TryGetSound(string profile,
                            UIAudioAction action,
                            out ActionSound sound)
    {
        if (lookup == null || lookup.Count == 0) BuildLookup();

        if (!string.IsNullOrEmpty(profile) &&
            lookup.TryGetValue(profile, out var dict) &&
            dict.TryGetValue(action, out sound))
        {
            return true;
        }

        // Fallback profile
        if (lookup.TryGetValue("Default", out var defaultDict) &&
            defaultDict.TryGetValue(action, out sound))
        {
            return true;
        }

        sound = default;
        return false;
    }

    #region Helpers
    void BuildLookup()
    {
        lookup = new Dictionary<string, Dictionary<UIAudioAction, ActionSound>>(
            profiles.Count,
            StringComparer.Ordinal);

        foreach (var p in profiles)
        {
            if (string.IsNullOrEmpty(p.ProfileName)) continue;

            var actionDict = new Dictionary<UIAudioAction, ActionSound>(p.Sounds.Count);
            foreach (var s in p.Sounds)
            {
                if (!actionDict.ContainsKey(s.Action))
                    actionDict[s.Action] = s;
            }

            lookup[p.ProfileName] = actionDict;
        }
    }
    #endregion
}
