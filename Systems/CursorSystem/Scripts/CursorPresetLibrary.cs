using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.CursorSystem
{
    /// <summary>
    /// Holds all cursor sets that can be referenced at runtime.
    /// Each set describes a collection of presets (different cursor visuals for actions).
    /// </summary>
    [CreateAssetMenu(fileName = "CursorPresetLibrary", menuName = "JGameFramework/Cursor System/Cursor Preset Library")]
    public sealed class CursorPresetLibrary : ScriptableObject
    {
        [SerializeField] List<CursorSetDefinition> cursorSets = new();

        public IReadOnlyList<CursorSetDefinition> CursorSets => cursorSets;

        public bool TryGetSet(string setId, out CursorSetDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(setId))
            {
                definition = null;
                return false;
            }

            for (int i = 0; i < cursorSets.Count; i++)
            {
                var candidate = cursorSets[i];
                if (candidate != null && candidate.MatchesId(setId))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public CursorSetDefinition GetFirstValidSet()
        {
            for (int i = 0; i < cursorSets.Count; i++)
            {
                var candidate = cursorSets[i];
                if (candidate != null)
                    return candidate;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class CursorSetDefinition
    {
        [SerializeField] string setId = "Default";
        [SerializeField] CursorPreset defaultPreset;
        [SerializeField] List<CursorPreset> cursorPresets = new();

        public string SetId => setId;
        public IReadOnlyList<CursorPreset> Presets => cursorPresets;

        public bool MatchesId(string candidate) =>
            !string.IsNullOrWhiteSpace(candidate) &&
            string.Equals(setId, candidate, StringComparison.OrdinalIgnoreCase);

        public bool TryGetPreset(string presetId, out CursorPreset preset)
        {
            if (!string.IsNullOrWhiteSpace(presetId))
            {
                if (defaultPreset != null && defaultPreset.MatchesId(presetId))
                {
                    preset = defaultPreset;
                    return true;
                }

                for (int i = 0; i < cursorPresets.Count; i++)
                {
                    var candidate = cursorPresets[i];
                    if (candidate != null && candidate.MatchesId(presetId))
                    {
                        preset = candidate;
                        return true;
                    }
                }
            }

            preset = null;
            return false;
        }

        public CursorPreset GetDefaultPreset()
        {
            if (defaultPreset != null && defaultPreset.HasTexture)
                return defaultPreset;

            for (int i = 0; i < cursorPresets.Count; i++)
            {
                var candidate = cursorPresets[i];
                if (candidate?.HasTexture == true)
                    return candidate;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class CursorPreset
    {
        [SerializeField] string presetId = "Default";
        [Tooltip("Cursor texture; ideally imported as Cursor with Read/Write enabled.")]
        [SerializeField] Texture2D cursorTexture;
        [Tooltip("Pixel offset of the mouse hot-spot inside the texture.")]
        [SerializeField] Vector2 hotSpot = Vector2.zero;
        [SerializeField] CursorMode cursorMode = CursorMode.Auto;
        [Tooltip("If enabled the preset dictates Cursor.visible when applied.")]
        [SerializeField] bool overrideCursorVisibility;
        [SerializeField] bool cursorVisible = true;

        public string PresetId => presetId;
        public Texture2D Texture => cursorTexture;
        public Vector2 HotSpot => hotSpot;
        public CursorMode Mode => cursorMode;
        public bool OverrideCursorVisibility => overrideCursorVisibility;
        public bool CursorVisible => cursorVisible;
        public bool HasTexture => cursorTexture != null;

        public bool MatchesId(string candidate) =>
            !string.IsNullOrWhiteSpace(candidate) &&
            string.Equals(presetId, candidate, StringComparison.OrdinalIgnoreCase);
    }
}
