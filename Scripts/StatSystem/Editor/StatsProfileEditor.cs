#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(StatsProfile))]
public class StatsProfileEditor : Editor
{
    private ReorderableList reorderableList;
    private SerializedProperty statEntriesProp;

    [Serializable] private class CopyDto { public string key; public float baseValue; }
    [Serializable] private class CopyWrapper { public List<CopyDto> items = new(); }
    private static string copyBuffer = "";

    void OnEnable()
    {
        statEntriesProp = serializedObject.FindProperty("statEntries");
        reorderableList = new ReorderableList(serializedObject, statEntriesProp, true, true, true, true);
        reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Stat Entries");
        reorderableList.drawElementCallback = (rect, index, isActive, isFocused)
            => EditorGUI.PropertyField(rect, statEntriesProp.GetArrayElementAtIndex(index), GUIContent.none);

        reorderableList.onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
        {
            var menu = new GenericMenu();

            var reg = StatRegistryProvider.Instance?.Registry;
            if (reg == null)
            {
                menu.AddDisabledItem(new GUIContent("No StatRegistry found"));
                menu.ShowAsContext();
                return;
            }

            var profile = (StatsProfile)target;
            profile.ResolveAll(); // ensure cache is up to date
            var existingKeys = new HashSet<string>(
                profile.statEntries.Select(e => e.statKey).Where(k => !string.IsNullOrWhiteSpace(k)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var def in EnumerateIStatDefinitions(reg))
            {
                if (def == null || string.IsNullOrWhiteSpace(def.Key)) continue;
                if (existingKeys.Contains(def.Key)) continue;

                string display = string.IsNullOrWhiteSpace(def.StatName) ? def.Key : def.StatName;
                menu.AddItem(new GUIContent(display), false, () =>
                {
                    Undo.RecordObject(profile, "Add Stat Entry");
                    var e = new StatsProfile.StatEntry
                    {
                        statKey = def.Key,
                        statDefinition = def,          // cached (not serialized)
                        baseValue = def.DefaultValue
                    };
                    profile.statEntries.Add(e);
                    EditorUtility.SetDirty(profile);
                    serializedObject.Update();
                    Repaint();
                });
            }

            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("All stats already added"));

            menu.ShowAsContext();
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Utilities
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Resolve Now"))
            {
                var profile = (StatsProfile)target;
                Undo.RecordObject(profile, "Resolve Stats");
                profile.ResolveAll();
                EditorUtility.SetDirty(profile);
            }

            if (GUILayout.Button("Remove Missing"))
            {
                var profile = (StatsProfile)target;
                var reg = StatRegistryProvider.Instance?.Registry;
                if (reg != null)
                {
                    Undo.RecordObject(profile, "Remove Missing Stats");
                    profile.statEntries.RemoveAll(e => string.IsNullOrWhiteSpace(e.statKey) || reg.Get(e.statKey) == null);
                    EditorUtility.SetDirty(profile);
                }
            }
        }

        reorderableList.DoLayoutList();

        GUILayout.Space(5);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Copy Entries"))
            {
                var profile = (StatsProfile)target;
                var wrapper = new CopyWrapper();
                foreach (var e in profile.statEntries)
                {
                    if (string.IsNullOrWhiteSpace(e.statKey)) continue;
                    wrapper.items.Add(new CopyDto { key = e.statKey, baseValue = e.baseValue });
                }
                copyBuffer = JsonUtility.ToJson(wrapper);
                Debug.Log("Copied stat entries (keys + base values).");
            }

            if (GUILayout.Button("Paste Entries"))
            {
                if (string.IsNullOrEmpty(copyBuffer))
                {
                    Debug.LogWarning("Nothing to paste—copy first.");
                }
                else
                {
                    try
                    {
                        var wrapper = JsonUtility.FromJson<CopyWrapper>(copyBuffer);
                        var profile = (StatsProfile)target;
                        var reg = StatRegistryProvider.Instance?.Registry;

                        if (reg == null)
                        {
                            Debug.LogWarning("StatRegistry not available.");
                        }
                        else
                        {
                            Undo.RecordObject(profile, "Paste Stat Entries");
                            var existing = new HashSet<string>(
                                profile.statEntries.Select(e => e.statKey).Where(k => !string.IsNullOrWhiteSpace(k)),
                                StringComparer.OrdinalIgnoreCase);

                            foreach (var dto in wrapper.items)
                            {
                                if (string.IsNullOrWhiteSpace(dto.key) || existing.Contains(dto.key))
                                    continue;

                                var def = reg.Get(dto.key);
                                if (def != null)
                                {
                                    profile.statEntries.Add(new StatsProfile.StatEntry
                                    {
                                        statKey = dto.key,
                                        statDefinition = def,
                                        baseValue = dto.baseValue
                                    });
                                    existing.Add(dto.key);
                                }
                                else
                                {
                                    Debug.LogWarning($"Unknown stat key '{dto.key}' during paste.");
                                }
                            }

                            EditorUtility.SetDirty(profile);
                            serializedObject.Update();
                            Repaint();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Paste failed: {ex.Message}");
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ---- helpers ----

    static IEnumerable<IStatDefinition> EnumerateIStatDefinitions(object registry)
    {
        if (registry == null) yield break;
        var type = registry.GetType();

        // Prefer "All"
        var allProp = type.GetProperty("All");
        if (allProp != null && typeof(IEnumerable).IsAssignableFrom(allProp.PropertyType))
        {
            foreach (var item in (IEnumerable)allProp.GetValue(registry))
                if (item is IStatDefinition idef) yield return idef;
            yield break;
        }

        // Fallback to legacy lists (if present)
        var statDefsProp = type.GetProperty("StatDefinitions");
        if (statDefsProp != null)
        {
            foreach (var item in (IEnumerable)statDefsProp.GetValue(registry))
                if (item is IStatDefinition idef) yield return idef;
            yield break;
        }

        var statDefsField = type.GetField("statDefinitions");
        if (statDefsField != null)
        {
            foreach (var item in (IEnumerable)statDefsField.GetValue(registry))
                if (item is IStatDefinition idef) yield return idef;
        }
    }
}
#endif
