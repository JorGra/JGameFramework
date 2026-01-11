using JGameFramework.Settings;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generic binding that keeps a UI Slider in sync with a persisted setting.
/// Can be reused in any screen that needs to display or edit settings.
/// </summary>
[RequireComponent(typeof(Slider))]
public class SettingSliderBinding : MonoBehaviour
{
    [SerializeField] private UserSettingKey settingKey;
    [SerializeField] private Slider slider;
    [SerializeField] private bool saveImmediately = true;

    void Awake()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }
    }

    void OnEnable()
    {
        UserSettings.EnsureInitialized();
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        UserSettings.OnFloatChanged += HandleSettingChanged;
        SyncFromStore();
    }

    void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        UserSettings.OnFloatChanged -= HandleSettingChanged;
    }

    /// <summary>
    /// Programmatically assign the setting key (useful when adding bindings from code).
    /// </summary>
    public void Configure(UserSettingKey key)
    {
        settingKey = key;
        SyncFromStore();
    }

    void SyncFromStore()
    {
        float value = UserSettings.GetFloat(settingKey);
        slider.SetValueWithoutNotify(value);
    }

    void OnSliderValueChanged(float value)
    {
        UserSettings.SetFloat(settingKey, value, saveImmediately);
    }

    void HandleSettingChanged(UserSettingKey key, float value)
    {
        if (key != settingKey) return;
        slider.SetValueWithoutNotify(value);
    }
}
