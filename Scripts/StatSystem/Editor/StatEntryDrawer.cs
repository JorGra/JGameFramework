#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Drawer for StatsProfile.StatEntry: shows resolved stat name (from key) + base value.
/// </summary>
[CustomPropertyDrawer(typeof(StatsProfile.StatEntry))]
public class StatEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var baseValueProp = property.FindPropertyRelative("baseValue");

        // Get the display label by resolving via the profile + index (works in edit & play mode)
        string statLabel = "Undefined Stat";
        var profile = property.serializedObject.targetObject as StatsProfile;
        var index = ExtractArrayIndex(property.propertyPath);

        if (profile != null && index >= 0 && index < profile.statEntries.Count)
        {
            var entry = profile.statEntries[index];
            var def = entry.Resolve(); // resolves from registry using entry.statKey
            statLabel = entry.DisplayName;
            // keep the cached pointer up to date so play-mode inspectors show names immediately
            profile.statEntries[index] = entry;
        }

        float half = position.width * 0.5f;
        var labelRect = new Rect(position.x, position.y, half, position.height);
        var fieldRect = new Rect(position.x + half, position.y, half, position.height);

        EditorGUI.LabelField(labelRect, statLabel);
        EditorGUI.PropertyField(fieldRect, baseValueProp, GUIContent.none);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;

    static int ExtractArrayIndex(string propertyPath)
    {
        const string token = "Array.data[";
        int i = propertyPath.IndexOf(token, StringComparison.Ordinal);
        if (i < 0) return -1;
        i += token.Length;
        int j = propertyPath.IndexOf(']', i);
        if (j < 0) return -1;
        return int.TryParse(propertyPath.Substring(i, j - i), out var idx) ? idx : -1;
    }
}
#endif
