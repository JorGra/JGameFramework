#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Appends a read-only “Runtime Stats” foldout to any MonoBehaviour
/// that implements IStatsProvider. Compatible with interface-based registry.
/// </summary>
[CustomEditor(typeof(MonoBehaviour), true)]
public class StatsProviderInspector : Editor
{
    bool _foldout = true;

    void OnEnable() => EditorApplication.update += Repaint;
    void OnDisable() => EditorApplication.update -= Repaint;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (target is not IStatsProvider provider || provider.Stats == null)
            return;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Start Play Mode to see live stat values.", MessageType.Info);
            return;
        }

        GUILayout.Space(6);
        _foldout = EditorGUILayout.Foldout(_foldout, "Runtime Stats", true);
        if (!_foldout) return;

        var reg = StatRegistryProvider.Instance?.Registry;
        if (reg == null)
        {
            EditorGUILayout.HelpBox("StatRegistry not available.", MessageType.Warning);
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            foreach (var def in EnumerateIStatDefinitions(reg))
            {
                if (def == null) continue;
                float value = provider.Stats.GetStat(def.Key);
                EditorGUILayout.FloatField(string.IsNullOrWhiteSpace(def.StatName) ? def.Key : def.StatName, value);
            }
        }
    }

    // Works with both the new and old registries:
    // - Prefers property "All : IEnumerable<IStatDefinition>"
    // - Falls back to property "StatDefinitions" or field "statDefinitions" (StatDefinition list)
    static IEnumerable<IStatDefinition> EnumerateIStatDefinitions(object registry)
    {
        if (registry == null) yield break;
        var type = registry.GetType();

        var allProp = type.GetProperty("All");
        if (allProp != null && typeof(IEnumerable).IsAssignableFrom(allProp.PropertyType))
        {
            foreach (var item in (IEnumerable)allProp.GetValue(registry))
                if (item is IStatDefinition idef) yield return idef;
            yield break;
        }

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
