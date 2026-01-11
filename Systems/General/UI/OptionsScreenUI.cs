using JGameFramework.Settings;
using UnityEngine;

public class OptionsScreenUI : UIPanelAnimatedSFX
{
    [Header("Audio Settings")]
    [SerializeField] private SettingSliderBinding masterVolume;
    [SerializeField] private SettingSliderBinding musicVolume;
    [SerializeField] private SettingSliderBinding sfxVolume;
    [SerializeField] private SettingSliderBinding uiVolume;

    protected override void Awake()
    {
        base.Awake();
        UserSettings.EnsureInitialized();

        ConfigureBinding(masterVolume, UserSettingKey.MasterVolume);
        ConfigureBinding(musicVolume, UserSettingKey.MusicVolume);
        ConfigureBinding(sfxVolume, UserSettingKey.EffectsVolume);
        ConfigureBinding(uiVolume, UserSettingKey.UIVolume);
    }

    static void ConfigureBinding(SettingSliderBinding binding, UserSettingKey key)
    {
        if (binding == null)
        {
            return;
        }

        binding.Configure(key);
    }
}
