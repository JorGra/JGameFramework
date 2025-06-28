#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(StatsProfile))]
public class StatsProfileEditor : Editor
{
    private ReorderableList reorderableList;
    private SerializedProperty statEntriesProp;
    private static string copyBuffer = "";

    private void OnEnable()
    {
        statEntriesProp = serializedObject.FindProperty("statEntries");
        reorderableList = new ReorderableList(serializedObject, statEntriesProp, true, true, true, true);
        reorderableList.drawHeaderCallback = DrawHeader;
        reorderableList.drawElementCallback = DrawListItems;

        // Use a dropdown callback to add new stat entries.
        reorderableList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
        {
            GenericMenu menu = new GenericMenu();
            // Get the global stat definitions from the registry.
            var provider = StatRegistryProvider.Instance;
            var registry = provider != null ? provider.Registry : null;
            if (registry != null)
            {
                foreach (var statDef in registry.statDefinitions)
                {
                    // Prevent duplicate entries by checking if the stat is already present.
                    bool alreadyPresent = false;
                    for (int i = 0; i < statEntriesProp.arraySize; i++)
                    {
                        SerializedProperty element = statEntriesProp.GetArrayElementAtIndex(i);
                        var existingStat = element.FindPropertyRelative("statDefinition").objectReferenceValue as StatDefinition;
                        if (existingStat == statDef)
                        {
                            alreadyPresent = true;
                            break;
                        }
                    }
                    if (!alreadyPresent)
                    {
                        menu.AddItem(new GUIContent(statDef.statName), false, () =>
                        {
                            int index = statEntriesProp.arraySize;
                            statEntriesProp.arraySize++;
                            SerializedProperty element = statEntriesProp.GetArrayElementAtIndex(index);
                            element.FindPropertyRelative("statDefinition").objectReferenceValue = statDef;
                            element.FindPropertyRelative("baseValue").floatValue = statDef.defaultValue;
                            serializedObject.ApplyModifiedProperties();
                        });
                    }
                }
                // If all stats are already present, disable the add option.
                if (menu.GetItemCount() == 0)
                {
                    menu.AddDisabledItem(new GUIContent("All global stats already added"));
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("No StatRegistry found"));
            }
            menu.ShowAsContext();
        };
    }

    private void DrawHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Stat Entries");
    }

    private void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = statEntriesProp.GetArrayElementAtIndex(index);
        // Use the custom property drawer (StatEntryDrawer) for each element.
        EditorGUI.PropertyField(rect, element, GUIContent.none);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        reorderableList.DoLayoutList();

        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy Entries"))
        {
            // Copy the entire asset as JSON.
            copyBuffer = EditorJsonUtility.ToJson(target);
            Debug.Log("Copied stat entries.");
        }
        if (GUILayout.Button("Paste Entries"))
        {
            if (!string.IsNullOrEmpty(copyBuffer))
            {
                EditorJsonUtility.FromJsonOverwrite(copyBuffer, target);
                EditorUtility.SetDirty(target);
                Debug.Log("Pasted stat entries.");
            }
            else
            {
                Debug.LogWarning("No stat entries copied. Please copy entries first.");
            }
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
