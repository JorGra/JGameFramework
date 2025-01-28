using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor for EffectorController that:
///  - Automatically detects/hides EffectBase components
///  - Displays them in a single list (with foldouts)
///  - Persists changes across Play mode/domain reload
///  - Allows reordering (Up/Down), removal (X), adding new effects
///  - Provides a "Play All Effects" button in Play mode
/// </summary>
[CustomEditor(typeof(EffectorController))]
public class EffectorControllerEditor : Editor
{
    private EffectorController controller;

    // Foldout states for each effect in the list (stored in EditorPrefs)
    private List<bool> foldouts = new List<bool>();

    // All possible effect types (subclasses of EffectBase)
    private List<Type> effectTypes;

    private void OnEnable()
    {
        controller = (EffectorController)target;

        // Ensure existing EffectBase components on this GameObject get hidden and added to the list
        SyncComponentsWithList();

        // Gather available effect types (for "Add Effect..." menu)
        CollectAllEffectTypes();

        // Sync foldouts to match the size of the controller's effect list
        SyncFoldoutListWithEffects();

        // Restore the foldout states from EditorPrefs
        RestoreFoldoutStates();
    }

    public override void OnInspectorGUI()
    {
        SyncComponentsWithList();

        // "Add Effect..." mini-button
        DrawAddEffectButton();

        // "Play All Effects" button (only active in Play mode)
        DrawPlayAllButton();

        EditorGUILayout.Space(8);

        // Draw the existing effects in a vertical listing
        DrawEffectListGUI();
    }

    #region Sync Components & Types

    /// <summary>
    /// Finds all EffectBase components on this GameObject, hides them in the inspector,
    /// and ensures they're present in the controller's 'effects' list.
    /// </summary>
    private void SyncComponentsWithList()
    {
        if (controller == null) return;

        // Find all EffectBase-based MonoBehaviours on this GameObject (excluding the controller if it ever derived from it)
        var mbEffects = controller.GetComponents<EffectBase>()
                                  .Where(eff => eff != null && eff.gameObject == controller.gameObject)
                                  .ToList();

        bool listChanged = false;

        // Hide them in the inspector and add them if not already in the list
        foreach (var eff in mbEffects)
        {
            if ((eff.hideFlags & HideFlags.HideInInspector) == 0)
            {
                eff.hideFlags = HideFlags.HideInInspector;
            }

            if (!controller.effects.Contains(eff))
            {
                Undo.RecordObject(controller, "Add Existing Effect");
                controller.effects.Add(eff);
                listChanged = true;
            }
        }

        // Remove references to null or missing components
        for (int i = controller.effects.Count - 1; i >= 0; i--)
        {
            var e = controller.effects[i];
            if (e == null || e.gameObject != controller.gameObject)
            {
                Undo.RecordObject(controller, "Remove Null/External Effect");
                controller.effects.RemoveAt(i);
                listChanged = true;
            }
        }

        if (listChanged)
        {
            EditorUtility.SetDirty(controller);
            SyncFoldoutListWithEffects();
        }
    }

    /// <summary>
    /// Collect all non-abstract classes that derive from EffectBase (for the "Add Effect..." dropdown).
    /// </summary>
    private void CollectAllEffectTypes()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var allTypes = new List<Type>();

        foreach (var asm in assemblies)
        {
            // Skip large system assemblies for performance
            if (asm.FullName.StartsWith("UnityEngine")) continue;
            if (asm.FullName.StartsWith("UnityEditor")) continue;
            if (asm.FullName.StartsWith("System")) continue;
            if (asm.FullName.StartsWith("mscorlib")) continue;

            try
            {
                allTypes.AddRange(asm.GetTypes());
            }
            catch { /* Some assemblies may fail reflection */ }
        }

        effectTypes = allTypes
            .Where(t => typeof(EffectBase).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && t.IsClass)
            .ToList();
    }

    /// <summary>
    /// Keep foldouts list in sync with the number of effects in the controller.
    /// </summary>
    private void SyncFoldoutListWithEffects()
    {
        if (controller.effects == null)
            controller.effects = new List<EffectBase>();

        while (foldouts.Count < controller.effects.Count)
            foldouts.Add(false);

        while (foldouts.Count > controller.effects.Count)
            foldouts.RemoveAt(foldouts.Count - 1);
    }

    #endregion

    #region Foldout Persistence

    private void RestoreFoldoutStates()
    {
        for (int i = 0; i < controller.effects.Count; i++)
        {
            var effectMb = controller.effects[i];
            if (effectMb == null) continue;

            string key = GetFoldoutKey(effectMb);
            int val = EditorPrefs.GetInt(key, 0);
            foldouts[i] = (val == 1);
        }
    }

    private void SaveFoldoutState(MonoBehaviour mb, bool state)
    {
        string key = GetFoldoutKey(mb);
        EditorPrefs.SetInt(key, state ? 1 : 0);
    }

    private string GetFoldoutKey(MonoBehaviour effectMb)
    {
        // Unique key for foldout
        return $"EffectorFoldout_{controller.GetInstanceID()}_{effectMb.GetInstanceID()}";
    }

    #endregion

    #region Add Effect

    private void DrawAddEffectButton()
    {
        if (GUILayout.Button("Add Effect...", EditorStyles.miniButton))
        {
            var menu = new GenericMenu();

            if (effectTypes == null || effectTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No EffectBase subclasses found"));
            }
            else
            {
                foreach (var t in effectTypes)
                {
                    var localType = t; // capture
                    menu.AddItem(new GUIContent(t.Name), false, () => OnAddEffectTypeSelected(localType));
                }
            }

            menu.ShowAsContext();
        }
    }

    private void OnAddEffectTypeSelected(Type effectType)
    {
        Undo.RecordObject(controller, "Add New Effect");

        // Create the new component as an EffectBase
        var newComp = Undo.AddComponent(controller.gameObject, effectType) as EffectBase;
        if (newComp != null)
        {
            newComp.hideFlags = HideFlags.HideInInspector;
            controller.AddEffect(newComp);
        }

        EditorUtility.SetDirty(controller);
        SyncFoldoutListWithEffects();
    }

    #endregion

    #region Play All Button

    private void DrawPlayAllButton()
    {
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("Play All Effects"))
        {
            controller.Play();
        }
        GUI.enabled = true;

        //if (!Application.isPlaying)
        //{
        //    EditorGUILayout.HelpBox("Enter Play mode to use 'Play All Effects'.", MessageType.Info);
        //}
    }

    #endregion

    #region Draw Effects

    private void DrawEffectListGUI()
    {
        if (controller.effects.Count == 0)
        {
            EditorGUILayout.HelpBox("No effects in the list.", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField("Existing Effects", EditorStyles.boldLabel);

        for (int i = 0; i < controller.effects.Count; i++)
        {
            var effectMb = controller.effects[i];
            if (effectMb == null)
            {
                DrawNullEffectEntry(i);
                continue;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.indentLevel++;

            bool oldState = foldouts[i];
            bool newState = EditorGUILayout.Foldout(oldState, effectMb.GetType().Name, true);
            if (newState != oldState)
            {
                foldouts[i] = newState;
                SaveFoldoutState(effectMb, newState);
            }

            // Move Up
            if (GUILayout.Button("\u25B2", GUILayout.Width(25))) // ▲
            {
                MoveEffectUp(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break; // break to avoid index issues
            }

            // Move Down
            if (GUILayout.Button("\u25BC", GUILayout.Width(25))) // ▼
            {
                MoveEffectDown(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            // Remove
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                RemoveEffectAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(3);

            // Foldout details
            if (foldouts[i])
            {
                EditorGUI.indentLevel++;
                DrawEffectProperties(effectMb);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(3);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }
    }

    private void DrawNullEffectEntry(int index)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Null (missing) effect reference", EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            Undo.RecordObject(controller, "Remove Null Effect");
            controller.effects.RemoveAt(index);
            EditorUtility.SetDirty(controller);
            SyncFoldoutListWithEffects();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEffectProperties(MonoBehaviour effectMb)
    {
        SerializedObject so = new SerializedObject(effectMb);
        so.Update();

        var prop = so.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            if (prop.name == "m_Script")
            {
                enterChildren = false;
                continue;
            }

            EditorGUILayout.PropertyField(prop, true);
            enterChildren = false;
        }

        so.ApplyModifiedProperties();
    }

    #endregion

    #region Reordering / Removing

    private void MoveEffectUp(int index)
    {
        if (index <= 0) return;
        Undo.RecordObject(controller, "Move Effect Up");

        var tmp = controller.effects[index];
        controller.effects[index] = controller.effects[index - 1];
        controller.effects[index - 1] = tmp;

        var foldTmp = foldouts[index];
        foldouts[index] = foldouts[index - 1];
        foldouts[index - 1] = foldTmp;

        EditorUtility.SetDirty(controller);
    }

    private void MoveEffectDown(int index)
    {
        if (index >= controller.effects.Count - 1) return;
        Undo.RecordObject(controller, "Move Effect Down");

        var tmp = controller.effects[index];
        controller.effects[index] = controller.effects[index + 1];
        controller.effects[index + 1] = tmp;

        var foldTmp = foldouts[index];
        foldouts[index] = foldouts[index + 1];
        foldouts[index + 1] = foldTmp;

        EditorUtility.SetDirty(controller);
    }

    private void RemoveEffectAt(int index)
    {
        if (index < 0 || index >= controller.effects.Count) return;
        Undo.RecordObject(controller, "Remove Effect");

        var effectMb = controller.effects[index];
        if (effectMb != null)
        {
            Undo.DestroyObjectImmediate(effectMb);
        }

        controller.effects.RemoveAt(index);
        EditorUtility.SetDirty(controller);
        SyncFoldoutListWithEffects();
    }

    #endregion
}
