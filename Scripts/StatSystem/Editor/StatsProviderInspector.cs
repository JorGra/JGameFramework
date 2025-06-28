#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generic inspector that appends a read-only “Runtime Stats” section
/// to every MonoBehaviour that implements IStatsProvider.
/// </summary>
[CustomEditor(typeof(MonoBehaviour), true)]
public class StatsProviderInspector : Editor
{
    bool _foldout = true;

    // Ensure the inspector refreshes while the game is running
    void OnEnable() => EditorApplication.update += Repaint;
    void OnDisable() => EditorApplication.update -= Repaint;

    public override void OnInspectorGUI()
    {
        // Draw the normal inspector first
        DrawDefaultInspector();

        // Only add the extra section if this component is a stats provider
        if (target is not IStatsProvider provider || provider.Stats == null)
            return;

        // Don’t spam the editor while not in play mode
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Start Play Mode to see live stat values.", MessageType.Info);
            return;
        }

        GUILayout.Space(6);
        _foldout = EditorGUILayout.Foldout(_foldout, "Runtime Stats", true);
        if (!_foldout) return;

        using (new EditorGUI.DisabledScope(true))
        {


            // Option B: without modifying Stats.cs
            var registry = StatRegistryProvider.Instance?.Registry;
            if (registry != null)
            {
                foreach (var def in registry.StatDefinitions)
                {
                    float value = provider.Stats.GetStat(def);
                    EditorGUILayout.FloatField(def.statName ?? def.key, value);
                }
            }

        }
    }
}
#endif
