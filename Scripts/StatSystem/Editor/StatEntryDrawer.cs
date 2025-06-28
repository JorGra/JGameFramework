#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom property drawer for StatsProfile.StatEntry.
/// Displays a dropdown-like label (the stat's name) on the left and a base value field on the right.
/// </summary>
[CustomPropertyDrawer(typeof(StatsProfile.StatEntry))]
public class StatEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Get the statDefinition and baseValue properties.
        SerializedProperty statDefinitionProp = property.FindPropertyRelative("statDefinition");
        SerializedProperty baseValueProp = property.FindPropertyRelative("baseValue");

        // Get the stat name from the statDefinition asset if available.
        string statName = "Undefined Stat";
        if (statDefinitionProp.objectReferenceValue != null)
        {
            StatDefinition statDef = statDefinitionProp.objectReferenceValue as StatDefinition;
            if (statDef != null)
            {
                statName = statDef.statName;
            }
        }

        // Split the drawing area: left for the stat name, right for the base value.
        Rect labelRect = new Rect(position.x, position.y, position.width * 0.5f, position.height);
        Rect fieldRect = new Rect(position.x + position.width * 0.5f, position.y, position.width * 0.5f, position.height);

        EditorGUI.LabelField(labelRect, statName);
        EditorGUI.PropertyField(fieldRect, baseValueProp, GUIContent.none);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
#endif
