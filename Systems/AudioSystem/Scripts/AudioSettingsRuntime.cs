using JGameFramework.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace JG.Audio
{

    /// <summary>
    /// Bridges persisted settings to the live audio systems.
    /// Runs automatically on startup and whenever settings change.
    /// </summary>
    public static class AudioSettingsRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Bootstrap()
        {
            UserSettings.EnsureInitialized();
            ApplyAudioSettings();
            UserSettings.OnFloatChanged += HandleSettingChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void HandleSettingChanged(UserSettingKey key, float value)
        {
            switch (key)
            {
                case UserSettingKey.MasterVolume:
                case UserSettingKey.MusicVolume:
                case UserSettingKey.EffectsVolume:
                case UserSettingKey.UIVolume:
                    ApplyAudioSettings();
                    break;
            }
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyAudioSettings();
        }

        public static void ApplyAudioSettings()
        {
            UserSettings.EnsureInitialized();

            float master = UserSettings.GetFloat(UserSettingKey.MasterVolume);
            float music = UserSettings.GetFloat(UserSettingKey.MusicVolume);
            float effects = UserSettings.GetFloat(UserSettingKey.EffectsVolume);
            float ui = UserSettings.GetFloat(UserSettingKey.UIVolume);

            AudioListener.volume = master;

            var soundManager = SoundManager.TryGetInstance();
            if (soundManager != null)
            {
                soundManager.SetChannelVolumes(master, music, effects, ui);
            }

            var backgroundMusic = BackgroundMusicManager.TryGetInstance();
            if (backgroundMusic != null)
            {
                backgroundMusic.ApplyVolumeSettings(master, music);
            }
        }
    }

}