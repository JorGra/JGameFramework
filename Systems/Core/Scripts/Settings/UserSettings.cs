using System;
using System.Collections.Generic;
using UnityEngine;

namespace JGameFramework.Settings
{
    /// <summary>Keys for user-facing, persisted settings.</summary>
    public enum UserSettingKey
    {
        MasterVolume,
        MusicVolume,
        EffectsVolume,
        UIVolume
    }

    /// <summary>
    /// Lightweight, reusable settings store backed by PlayerPrefs.
    /// Supports strongly-typed keys, default values, clamping and change notifications.
    /// </summary>
    public static class UserSettings
    {
        public delegate void FloatSettingChanged(UserSettingKey key, float value);

        /// <summary>Raised whenever a float setting changes (after clamping & persistence).</summary>
        public static event FloatSettingChanged OnFloatChanged;

        static readonly Dictionary<UserSettingKey, FloatSetting> floatSettings = new()
        {
            { UserSettingKey.MasterVolume, new FloatSetting("settings.master", 1f, 0f, 1f) },
            { UserSettingKey.MusicVolume, new FloatSetting("settings.music", 1f, 0f, 1f) },
            { UserSettingKey.EffectsVolume, new FloatSetting("settings.effects", 1f, 0f, 1f) },
            { UserSettingKey.UIVolume, new FloatSetting("settings.ui", 1f, 0f, 1f) }
        };

        static bool initialized;

        /// <summary>Load persisted values once per session.</summary>
        public static void EnsureInitialized()
        {
            if (initialized) return;

            foreach (var kvp in floatSettings)
            {
                kvp.Value.Load();
            }

            initialized = true;
        }

        public static float GetFloat(UserSettingKey key)
        {
            EnsureInitialized();
            return floatSettings[key].Value;
        }

        public static void SetFloat(UserSettingKey key, float value, bool saveImmediately = true)
        {
            EnsureInitialized();

            if (floatSettings.TryGetValue(key, out var setting) && setting.Apply(value, saveImmediately))
            {
                OnFloatChanged?.Invoke(key, setting.Value);
            }
        }

        public static void Save() => PlayerPrefs.Save();

        #region Nested types
        sealed class FloatSetting
        {
            readonly string prefsKey;
            readonly float defaultValue;
            readonly float min;
            readonly float max;

            public float Value { get; private set; }

            public FloatSetting(string prefsKey, float defaultValue, float min, float max)
            {
                this.prefsKey = prefsKey;
                this.defaultValue = defaultValue;
                this.min = min;
                this.max = max;
                Value = defaultValue;
            }

            public void Load()
            {
                Value = PlayerPrefs.GetFloat(prefsKey, defaultValue);
                Value = Mathf.Clamp(Value, min, max);
            }

            public bool Apply(float value, bool saveImmediately)
            {
                float clamped = Mathf.Clamp(value, min, max);
                if (Mathf.Approximately(Value, clamped))
                {
                    return false;
                }

                Value = clamped;
                PlayerPrefs.SetFloat(prefsKey, Value);
                if (saveImmediately)
                {
                    PlayerPrefs.Save();
                }
                return true;
            }
        }
        #endregion
    }
}
