// ThemeAssetEditor.cs
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UI.Theming.Editor
{
    /// <summary>
    /// Reorderable, horizontal list inspector for <see cref="ThemeAsset"/>.
    /// • Green dot     – entry overrides a key in Base Theme<br/>
    /// • “Add ▼” button– pick a key from Base Theme to override
    /// </summary>
    [CustomEditor(typeof(ThemeAsset), true)]
    public sealed class ThemeAssetEditor : UnityEditor.Editor
    {
        static readonly Color dotColor = new(0.35f, 0.8f, 0.35f, 1f);
        const float dotSize = 8f;    // diameter
        const float btnSize = 18f;   // remove ✕ button width

        ReorderableList colorsList;
        ReorderableList spritesList;
        ReorderableList fontsList;

        void OnEnable()
        {
            colorsList = BuildList("colors", "Colours");
            spritesList = BuildList("sprites", "Sprites");
            fontsList = BuildList("fonts", "Fonts");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseTheme"));
            EditorGUILayout.Space(4);

            colorsList.DoLayoutList();
            spritesList.DoLayoutList();
            fontsList.DoLayoutList();

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("styleSheets"), true);

            serializedObject.ApplyModifiedProperties();
        }

        #region ───────────────────── list builder ─────────────────────
        ReorderableList BuildList(string propertyName, string header)
        {
            var listProp = serializedObject.FindProperty(propertyName);

            var rl = new ReorderableList(serializedObject, listProp, true, true, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 4
            };

            // ───── header with “Add ▼” button
            rl.drawHeaderCallback = rect =>
            {
                const float addWidth = 55f;
                var labelRect = new Rect(rect.x, rect.y, rect.width - addWidth, rect.height);
                EditorGUI.LabelField(labelRect, header, EditorStyles.boldLabel);

                var addRect = new Rect(rect.x + rect.width - addWidth, rect.y + 1, addWidth - 4f, rect.height - 2f);
                if (GUI.Button(addRect, "Add ▼", EditorStyles.miniButton))
                {
                    ShowAddMenu(listProp, propertyName);
                }
            };

            // ───── element rows
            rl.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = listProp.GetArrayElementAtIndex(index);
                var keyProp = element.FindPropertyRelative("key");

                bool overrides = IsOverride(propertyName, keyProp?.stringValue);

                rect.y += 2f;
                float keyWidth = rect.width * 0.4f;
                float valueWidth = rect.width - keyWidth - btnSize - dotSize - 10f;

                // Key
                var keyRect = new Rect(rect.x, rect.y, keyWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);

                // Value
                var valRect = new Rect(keyRect.xMax + 2f, rect.y, valueWidth, EditorGUIUtility.singleLineHeight);
                var valProp = FindValueProp(element);
                if (valProp != null) EditorGUI.PropertyField(valRect, valProp, GUIContent.none);

                // Dot
                if (overrides)
                {
                    var centre = new Vector2(valRect.xMax + dotSize * 0.5f + 4f,
                                             rect.y + EditorGUIUtility.singleLineHeight * 0.5f);
                    Handles.color = dotColor;
                    Handles.DrawSolidDisc(centre, Vector3.forward, dotSize * 0.5f);
                }

                // Remove ✕
                var btnRect = new Rect(rect.x + rect.width - btnSize, rect.y, btnSize, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(btnRect, "✕"))
                {
                    listProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            };

            // Ensure delete via list menu works
            rl.onRemoveCallback = l =>
            {
                l.serializedProperty.DeleteArrayElementAtIndex(l.index);
                serializedObject.ApplyModifiedProperties();
            };

            return rl;
        }
        #endregion

        #region ───────────────────── add-menu logic ─────────────────────
        void ShowAddMenu(SerializedProperty listProp, string propertyName)
        {
            var theme = (ThemeAsset)target;
            var baseTheme = typeof(ThemeAsset)
                .GetField("baseTheme", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(theme) as ThemeAsset;

            if (baseTheme == null)
            {
                EditorUtility.DisplayDialog("No Base Theme", "Assign a Base Theme first.", "OK");
                return;
            }

            // Gather keys already present
            var existingKeys = new HashSet<string>();
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var kp = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("key");
                if (kp != null) existingKeys.Add(kp.stringValue);
            }

            // Gather keys from base list
            var baseField = typeof(ThemeAsset)
                .GetField(propertyName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var menu = new GenericMenu();
            bool any = false;

            if (baseField?.GetValue(baseTheme) is System.Collections.IEnumerable baseList)
            {
                foreach (var entry in baseList)
                {
                    var keyField = entry.GetType().GetField("key");
                    if (keyField == null) continue;

                    string key = (string)keyField.GetValue(entry);
                    any = true;

                    if (existingKeys.Contains(key))
                        menu.AddDisabledItem(new GUIContent(key));
                    else
                        menu.AddItem(new GUIContent(key), false, () =>
                        {
                            AddOverrideElement(listProp, key);
                        });
                }
            }

            if (!any) menu.AddDisabledItem(new GUIContent("(base theme empty)"));
            menu.ShowAsContext();
        }

        void AddOverrideElement(SerializedProperty listProp, string key)
        {
            // Resolve fresh property (context menu executes later)
            var prop = listProp.serializedObject.FindProperty(listProp.propertyPath);
            prop.serializedObject.Update();

            prop.InsertArrayElementAtIndex(prop.arraySize);
            var newElement = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            newElement.FindPropertyRelative("key").stringValue = key;

            // Give value a sensible default
            var val = FindValueProp(newElement);
            if (val != null)
            {
                if (val.propertyType == SerializedPropertyType.Color) val.colorValue = Color.white;
                else val.objectReferenceValue = null;
            }

            prop.serializedObject.ApplyModifiedProperties();
        }
        #endregion

        #region ───────────────────── helpers ─────────────────────
        static SerializedProperty FindValueProp(SerializedProperty element)
        {
            foreach (SerializedProperty child in element.Copy())
            {
                if (child.name is "color" or "sprite" or "font") return child;
            }
            return null;
        }

        bool IsOverride(string listName, string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            var theme = (ThemeAsset)target;
            var baseTheme = typeof(ThemeAsset)
                .GetField("baseTheme", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(theme) as ThemeAsset;

            if (baseTheme == null) return false;

            var listField = typeof(ThemeAsset)
                .GetField(listName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (listField?.GetValue(baseTheme) is System.Collections.IEnumerable baseList)
            {
                foreach (var entry in baseList)
                {
                    var kf = entry.GetType().GetField("key");
                    if (kf != null && (string)kf.GetValue(entry) == key) return true;
                }
            }
            return false;
        }
        #endregion
    }
}
