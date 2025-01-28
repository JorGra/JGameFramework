using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// Custom editor for EffectorController that:
///  - Automatically detects/hides IEffect components
///  - Displays them in a single list (with foldouts)
///  - Persists changes (and the foldout states) across Play mode/domain reload
///  - Allows reordering (Up/Down), removal (X), adding new effects
///  - Provides a "Play All Effects" button in Play mode
/// </summary>
[CustomEditor(typeof(EffectorController))]
public class EffectorControllerEditor : Editor
{
    private EffectorController controller;

    // Foldout states for each effect in the list. We also store them in EditorPrefs for persistence.
    private List<bool> foldouts = new List<bool>();

    // All possible effect types (MonoBehaviours implementing IEffect)
    private List<Type> effectTypes;

    /// <summary>
    /// Called when the editor is enabled/reloaded. We set up references, sync the components,
    /// gather types, and restore the foldout states.
    /// </summary>
    private void OnEnable()
    {
        controller = (EffectorController)target;

        // Ensure existing effect components on this GameObject get hidden and added to the list
        SyncComponentsWithList();

        // Gather available effect types (for "Add Effect..." menu)
        CollectAllEffectTypes();

        // Sync the foldout list to match the size of the controller's effect list
        SyncFoldoutListWithEffects();

        // Restore persisted foldout states from EditorPrefs
        RestoreFoldoutStates();
    }

    public override void OnInspectorGUI()
    {
        // Continuously ensure the EffectorController's list matches actual MonoBehaviours on the object
        // (in case the user manually adds/removes components in other ways).
        SyncComponentsWithList();

        // Draw a single "Add Effect..." button that shows a dropdown of possible effect types
        DrawAddEffectButton();

        // "Play All Effects" button (only active in Play mode)
        DrawPlayAllButton();

        EditorGUILayout.Space(8);

        // Draw the existing effects in a vertical listing
        DrawEffectListGUI();
    }

    #region Sync Components & Types

    /// <summary>
    /// Ensures that any MonoBehaviour on this GameObject that implements IEffect
    /// is in the controller.effects list, and hides them from the default inspector.
    /// </summary>
    private void SyncComponentsWithList()
    {
        if (controller == null) return;

        var mbEffects = controller.GetComponents<MonoBehaviour>()
            .Where(mb => mb is IEffect && mb != controller)
            .Cast<IEffect>()
            .ToList();

        // Hide all existing IEffect-based MonoBehaviours so they don't appear as normal components
        bool listChanged = false;
        foreach (var eff in mbEffects)
        {
            if (eff is MonoBehaviour effMb)
            {
                if ((effMb.hideFlags & HideFlags.HideInInspector) == 0)
                {
                    effMb.hideFlags = HideFlags.HideInInspector;
                }
            }

            // If not already in the controller's list, add it
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
            if (e == null)
            {
                Undo.RecordObject(controller, "Remove Null Effect");
                controller.effects.RemoveAt(i);
                listChanged = true;
            }
            else
            {
                // If it's a MonoBehaviour, ensure it's on *this* GameObject
                if (e is MonoBehaviour effMb && effMb.gameObject != controller.gameObject)
                {
                    Undo.RecordObject(controller, "Remove External Effect");
                    controller.effects.RemoveAt(i);
                    listChanged = true;
                }
            }
        }

        if (listChanged)
        {
            EditorUtility.SetDirty(controller);
            SyncFoldoutListWithEffects();
        }
    }

    /// <summary>
    /// Collect all non-abstract MonoBehaviours implementing IEffect across user assemblies.
    /// This is for the "Add Effect..." dropdown.
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
            catch { /* Some assemblies fail reflection, skip */ }
        }

        effectTypes = allTypes
            .Where(t => typeof(IEffect).IsAssignableFrom(t)
                        && typeof(MonoBehaviour).IsAssignableFrom(t)
                        && !t.IsAbstract)
            .ToList();
    }

    /// <summary>
    /// Ensures our foldouts list is the same size as the effect list.
    /// </summary>
    private void SyncFoldoutListWithEffects()
    {
        if (controller.effects == null)
            controller.effects = new List<IEffect>();

        while (foldouts.Count < controller.effects.Count)
            foldouts.Add(false);

        while (foldouts.Count > controller.effects.Count)
            foldouts.RemoveAt(foldouts.Count - 1);
    }

    #endregion

    #region Foldout Persistence

    /// <summary>
    /// Restores the foldout states from EditorPrefs for each effect in the list.
    /// Called once in OnEnable().
    /// </summary>
    private void RestoreFoldoutStates()
    {
        for (int i = 0; i < controller.effects.Count; i++)
        {
            var effect = controller.effects[i];
            if (effect is MonoBehaviour mb)
            {
                string key = GetFoldoutKey(mb);
                int val = EditorPrefs.GetInt(key, 0);
                foldouts[i] = (val == 1);
            }
        }
    }

    /// <summary>
    /// Saves the foldout state of a particular effect's index to EditorPrefs.
    /// Called when the user toggles a foldout in the inspector.
    /// </summary>
    private void SaveFoldoutState(MonoBehaviour mb, bool state)
    {
        string key = GetFoldoutKey(mb);
        EditorPrefs.SetInt(key, state ? 1 : 0);
    }

    /// <summary>
    /// Generates a unique key for storing foldout state per effect,
    /// based on the controller and the MonoBehaviour instance IDs.
    /// </summary>
    private string GetFoldoutKey(MonoBehaviour effectMb)
    {
        // Something like: "EffectorFoldout_[ControllerID]_[EffectID]"
        return $"EffectorFoldout_{controller.GetInstanceID()}_{effectMb.GetInstanceID()}";
    }

    #endregion

    #region Add Effect Button

    /// <summary>
    /// Renders a single "Add Effect..." mini-button that opens a drop-down menu.
    /// </summary>
    private void DrawAddEffectButton()
    {
        if (GUILayout.Button("Add Effect...", EditorStyles.miniButton))
        {
            var menu = new GenericMenu();

            if (effectTypes == null || effectTypes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No IEffect MonoBehaviours found"));
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

    /// <summary>
    /// Called when the user picks an effect type in the "Add Effect..." drop-down.
    /// Creates a new hidden component and adds it to the list.
    /// </summary>
    private void OnAddEffectTypeSelected(Type effectType)
    {
        Undo.RecordObject(controller, "Add New Effect");

        // Create the new MonoBehaviour with Undo so user can Undo/Redo
        var newComp = Undo.AddComponent(controller.gameObject, effectType) as IEffect;

        if (newComp is MonoBehaviour mb)
        {
            mb.hideFlags = HideFlags.HideInInspector;
        }

        // Insert into the list
        controller.AddEffect(newComp);
        EditorUtility.SetDirty(controller);

        SyncFoldoutListWithEffects();
    }

    #endregion

    #region "Play All Effects" Button

    /// <summary>
    /// Displays a button labeled "Play All Effects", only clickable if we are in Play mode.
    /// If pressed, calls controller.Play().
    /// </summary>
    private void DrawPlayAllButton()
    {
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("Play All Effects"))
        {
            controller.Play();
        }
        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode to use 'Play All Effects'.", MessageType.Info);
        }
    }

    #endregion

    #region Drawing the Effect List

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
            var effect = controller.effects[i];
            if (effect == null)
            {
                DrawNullEffectEntry(i);
                continue;
            }

            if (effect is MonoBehaviour effectMb)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.Space(3);

                // Row: foldout, reorder buttons, remove button
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

                // If foldout is open, draw all serialized fields of the effect
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
            else
            {
                DrawFallbackEffectEntry(i, effect);
            }
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

    private void DrawFallbackEffectEntry(int index, IEffect effect)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Non-MonoBehaviour effect: {effect.GetType().Name}");
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            RemoveEffectAt(index);
        }
        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Effect Property Drawing

    /// <summary>
    /// Draws all serialized properties of a MonoBehaviour-based effect,
    /// except 'm_Script'.
    /// </summary>
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
                // Skip the default script reference
                enterChildren = false;
                continue;
            }

            EditorGUILayout.PropertyField(prop, true);
            enterChildren = false; // only expand children of the first property
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

        // Swap foldout states as well
        var fold = foldouts[index];
        foldouts[index] = foldouts[index - 1];
        foldouts[index - 1] = fold;

        EditorUtility.SetDirty(controller);
    }

    private void MoveEffectDown(int index)
    {
        if (index >= controller.effects.Count - 1) return;
        Undo.RecordObject(controller, "Move Effect Down");

        var tmp = controller.effects[index];
        controller.effects[index] = controller.effects[index + 1];
        controller.effects[index + 1] = tmp;

        var fold = foldouts[index];
        foldouts[index] = foldouts[index + 1];
        foldouts[index + 1] = fold;

        EditorUtility.SetDirty(controller);
    }

    private void RemoveEffectAt(int index)
    {
        if (index < 0 || index >= controller.effects.Count) return;

        var effect = controller.effects[index];
        Undo.RecordObject(controller, "Remove Effect");

        // If it's a MonoBehaviour, destroy the component from the GameObject
        if (effect is MonoBehaviour mb)
        {
            Undo.DestroyObjectImmediate(mb);
        }

        // Remove from list
        controller.effects.RemoveAt(index);

        EditorUtility.SetDirty(controller);
        SyncFoldoutListWithEffects();
    }

    #endregion
}
