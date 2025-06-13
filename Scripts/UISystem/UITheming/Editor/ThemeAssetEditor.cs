// ThemeAssetEditor.cs
// Unity 2022.3 • URP
//
// Custom inspector for ThemeAsset with typed StyleSheet overrides.
//
// • Colours / Sprites / Fonts  → “Add ▼” menu clones missing keys from Base Theme.
// • Style Sheets               → “Add Override ▼” clones an entire style from Base Theme
//                                into the correct local sheet (creating it if absent).
// • Green dot shows entries that override a value in the parent theme.
//

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UI.Theming.Editor
{
    [CustomEditor(typeof(ThemeAsset), true)]
    public sealed class ThemeAssetEditor : UnityEditor.Editor
    {
        // ───────────────────────────────────────── constants ─────────────────────
        static readonly Color dotColor = new(0.35f, 0.8f, 0.35f, 1f);
        const float dotSize = 8f;
        const float btnSize = 18f;

        // ───────────────────────────────────────── state ─────────────────────────
        ReorderableList colorsList, spritesList, fontsList;
        SerializedProperty? styleSheetsProp;
        readonly Dictionary<string, bool> spriteFoldouts = new();
        void OnEnable()
        {
            colorsList = BuildList("colors", "Colours");
            spritesList = BuildSpriteList();
            fontsList = BuildList("fonts", "Fonts");

            styleSheetsProp = serializedObject.FindProperty("styleSheets");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseTheme"));
            EditorGUILayout.Space(4);

            colorsList.DoLayoutList();
            spritesList.DoLayoutList();
            fontsList.DoLayoutList();

            DrawStyleSheetsHeader();
            if (styleSheetsProp != null)
            {
                EditorGUILayout.PropertyField(styleSheetsProp, true);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ────────────────────────── style-sheet override UI ──────────────────────
        void DrawStyleSheetsHeader()
        {
            const float addWidth = 95f;

            var rect = EditorGUILayout.GetControlRect(false);
            var labelRect = new Rect(rect.x, rect.y, rect.width - addWidth, rect.height);
            EditorGUI.LabelField(labelRect, "Style Sheets", EditorStyles.boldLabel);

            var addRect = new Rect(rect.x + rect.width - addWidth, rect.y, addWidth, rect.height);
            if (GUI.Button(addRect, "Add Override ▼", EditorStyles.miniButton))
                ShowAddStyleOverrideMenu();
        }

        void ShowAddStyleOverrideMenu()
        {
            var theme = (ThemeAsset)target;
            var baseTheme = GetBaseTheme(theme);
            if (baseTheme == null)
            {
                EditorUtility.DisplayDialog("No Base Theme",
                                            "Assign a Base Theme first to override styles.",
                                            "OK");
                return;
            }

            // (sheetType, key) pairs already present locally
            var existing = new HashSet<(Type, string)>();
            foreach (var sheet in GetSheets(theme))
                foreach (var style in GetStyles(sheet))
                    existing.Add((sheet.GetType(), style.StyleKey));

            var menu = new GenericMenu();
            bool any = false;

            foreach (var parentSheet in GetSheets(baseTheme))
            {
                string sheetGroup = parentSheet.GetType().Name.Replace("Sheet", ""); // e.g. TextStyle
                Type sheetType = parentSheet.GetType();

                foreach (var parentStyle in GetStyles(parentSheet))
                {
                    string styleKey = parentStyle.StyleKey;
                    string path = $"{sheetGroup}/{styleKey}";
                    any = true;

                    if (existing.Contains((sheetType, styleKey)))
                    {
                        menu.AddDisabledItem(new GUIContent(path));
                    }
                    else
                    {
                        // capture for lambda
                        var cachedSheetType = sheetType;
                        var cachedParentStyle = parentStyle;

                        menu.AddItem(new GUIContent(path), false,
                                     () => AddOverrideStyle(cachedSheetType, cachedParentStyle));
                    }
                }
            }

            if (!any) menu.AddDisabledItem(new GUIContent("(nothing to override)"));
            menu.ShowAsContext();
        }

        // Creates (or re-uses) the proper sheet and clones the parent style into it
        void AddOverrideStyle(Type sheetType, StyleModuleParameters parentStyle)
        {
            if (styleSheetsProp == null) return;
            serializedObject.Update();

            // 1. find or create the sheet
            object? sheetObj = null;
            for (int i = 0; i < styleSheetsProp.arraySize; i++)
            {
                var elem = styleSheetsProp.GetArrayElementAtIndex(i);
                if (elem.managedReferenceValue?.GetType() == sheetType)
                {
                    sheetObj = elem.managedReferenceValue;
                    break;
                }
            }

            if (sheetObj == null)
            {
                styleSheetsProp.InsertArrayElementAtIndex(styleSheetsProp.arraySize);
                var newProp = styleSheetsProp.GetArrayElementAtIndex(styleSheetsProp.arraySize - 1);
                sheetObj = Activator.CreateInstance(sheetType);
                newProp.managedReferenceValue = sheetObj;
            }

            // 2. grab the private List<T> 'styles' and add a clone
            var stylesField = FindStylesField(sheetType);
            if (stylesField == null)
            {
                Debug.LogError($"{sheetType.Name} is missing private List<T> 'styles' field.");
                return;
            }

            var list = (IList)stylesField.GetValue(sheetObj)!;
            var clone = Activator.CreateInstance(parentStyle.GetType())!;
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(parentStyle), clone);
            list.Add(clone);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        // ─────────── colour / sprite / font lists (unchanged except minor tidy) ───
        ReorderableList BuildList(string propertyName, string header)
        {
            var listProp = serializedObject.FindProperty(propertyName);

            var rl = new ReorderableList(serializedObject, listProp, true, true, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 4
            };

            // header: label + “Add ▼”
            rl.drawHeaderCallback = rect =>
            {
                const float addWidth = 55f;
                var labelRect = new Rect(rect.x, rect.y, rect.width - addWidth, rect.height);
                EditorGUI.LabelField(labelRect, header, EditorStyles.boldLabel);

                var addRect = new Rect(rect.x + rect.width - addWidth, rect.y + 1,
                                       addWidth - 4f, rect.height - 2f);

                if (GUI.Button(addRect, "Add ▼", EditorStyles.miniButton))
                    ShowAddMenu(listProp, propertyName);
            };

            // each element row
            rl.drawElementCallback = (rect, index, _, _) =>
            {
                var element = listProp.GetArrayElementAtIndex(index);
                var keyProp = element.FindPropertyRelative("key");
                if (keyProp == null) return;

                bool overrides = IsOverride(propertyName, keyProp.stringValue);

                rect.y += 2f;
                float keyW = rect.width * 0.4f;
                float valW = rect.width - keyW - btnSize - dotSize - 10f;

                // key
                var keyRect = new Rect(rect.x, rect.y, keyW, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);

                // value
                var valRect = new Rect(keyRect.xMax + 2f, rect.y, valW,
                                       EditorGUIUtility.singleLineHeight);
                var valProp = FindValueProp(element);
                if (valProp != null)
                    EditorGUI.PropertyField(valRect, valProp, GUIContent.none);

                // dot
                if (overrides)
                {
                    var centre = new Vector2(valRect.xMax + dotSize * 0.5f + 4f,
                                             rect.y + EditorGUIUtility.singleLineHeight * 0.5f);
                    Handles.color = dotColor;
                    Handles.DrawSolidDisc(centre, Vector3.forward, dotSize * 0.5f);
                }

                // delete ✕
                var btnRect = new Rect(rect.x + rect.width - btnSize,
                                       rect.y,
                                       btnSize,
                                       EditorGUIUtility.singleLineHeight);
                if (GUI.Button(btnRect, "✕"))
                {
                    listProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            };

            rl.onRemoveCallback = l =>
            {
                l.serializedProperty.DeleteArrayElementAtIndex(l.index);
                serializedObject.ApplyModifiedProperties();
            };

            return rl;
        }

        ReorderableList BuildSpriteList()
        {
            var listProp = serializedObject.FindProperty("sprites");

            var rl = new ReorderableList(serializedObject, listProp, true, true, true, true)
            {
                // variable height – 1 or 2 rows
                elementHeightCallback = i =>
                {
                    var key = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue;
                    return (spriteFoldouts.TryGetValue(key, out var open) && open)
                           ? EditorGUIUtility.singleLineHeight * 3 + 8
                           : EditorGUIUtility.singleLineHeight + 4;
                }
            };

            rl.drawHeaderCallback = rect =>
            {
                const float addWidth = 55f;
                var labelRect = new Rect(rect.x, rect.y, rect.width - addWidth, rect.height);
                EditorGUI.LabelField(labelRect, "Sprites", EditorStyles.boldLabel);

                var addRect = new Rect(rect.x + rect.width - addWidth, rect.y + 1,
                                       addWidth - 4f, rect.height - 2f);
                if (GUI.Button(addRect, "Add ▼", EditorStyles.miniButton))
                    ShowAddMenu(listProp, "sprites");
            };

            rl.drawElementCallback = (rect, index, _, _) =>
            {
                var element = listProp.GetArrayElementAtIndex(index);
                var keyProp = element.FindPropertyRelative("key");
                var spriteProp = element.FindPropertyRelative("sprite");
                var typeProp = element.FindPropertyRelative("imageType");
                var ppuProp = element.FindPropertyRelative("pixelPerUnit");

                var key = keyProp.stringValue;
                spriteFoldouts.TryGetValue(key, out var open);

                // fold-out triangle
                var foldRect = new Rect(rect.x + 10, rect.y + 2f, 5f, EditorGUIUtility.singleLineHeight);
                open = EditorGUI.Foldout(foldRect, open, GUIContent.none);
                spriteFoldouts[key] = open;

                // key field
                float keyW = rect.width * 0.3f;
                var keyRect = new Rect(foldRect.xMax + 2f, rect.y + 2f, keyW, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);

                // sprite field
                float sprW = rect.width - keyW - 12f - 20f - 10; // leave room for ×
                var sprRect = new Rect(keyRect.xMax + 2f, rect.y + 2f, sprW,
                                       EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(sprRect, spriteProp, GUIContent.none);

                // delete button
                var btnRect = new Rect(rect.x + rect.width - 18f, rect.y + 2f, 18f,
                                       EditorGUIUtility.singleLineHeight);
                if (GUI.Button(btnRect, "✕"))
                {
                    listProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }

                // second row – Image Type
                if (open)
                {
                    // line-2:  Image Type
                    var typeRect = new Rect(rect.x + 16f,
                                            rect.y + EditorGUIUtility.singleLineHeight + 6f,
                                            rect.width - 16f,
                                            EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("Image Type"));

                    // line-3:  Pixel Per Unit (directly underneath)
                    var ppuRect = new Rect(typeRect.x,
                                           typeRect.y + EditorGUIUtility.singleLineHeight + 2f,
                                           typeRect.width,
                                           EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(ppuRect, ppuProp, new GUIContent("Pixels Per Unit"));
                }
            };

            rl.onRemoveCallback = l =>
            {
                l.serializedProperty.DeleteArrayElementAtIndex(l.index);
                serializedObject.ApplyModifiedProperties();
            };

            return rl;
        }

        void ShowAddMenu(SerializedProperty listProp, string propertyName)
        {
            var theme = (ThemeAsset)target;
            var baseTheme = GetBaseTheme(theme);
            if (baseTheme == null)
            {
                EditorUtility.DisplayDialog("No Base Theme",
                                            "Assign a Base Theme first to override entries.",
                                            "OK");
                return;
            }

            // local keys
            var existing = new HashSet<string>();
            for (int i = 0; i < listProp.arraySize; i++)
            {
                var kp = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("key");
                if (kp != null) existing.Add(kp.stringValue);
            }

            var baseField = typeof(ThemeAsset)
                .GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);

            var menu = new GenericMenu();
            bool any = false;

            if (baseField?.GetValue(baseTheme) is IEnumerable baseList)
            {
                foreach (var entry in baseList)
                {
                    var keyField = entry.GetType().GetField("key");
                    if (keyField == null) continue;

                    string key = (string)keyField.GetValue(entry);
                    any = true;

                    if (existing.Contains(key))
                        menu.AddDisabledItem(new GUIContent(key));
                    else
                        menu.AddItem(new GUIContent(key), false,
                                     () => AddOverrideElement(listProp, key));
                }
            }

            if (!any) menu.AddDisabledItem(new GUIContent("(base theme empty)"));
            menu.ShowAsContext();
        }

        void AddOverrideElement(SerializedProperty listProp, string key)
        {
            var prop = listProp.serializedObject.FindProperty(listProp.propertyPath);
            prop.serializedObject.Update();

            prop.InsertArrayElementAtIndex(prop.arraySize);
            var newEl = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            newEl.FindPropertyRelative("key").stringValue = key;

            var val = FindValueProp(newEl);
            if (val != null)
            {
                if (val.propertyType == SerializedPropertyType.Color)
                    val.colorValue = Color.white;
                else
                    val.objectReferenceValue = null;
            }

            prop.serializedObject.ApplyModifiedProperties();
        }

        // ───────────────────────────── reflection helpers ─────────────────────────
        static FieldInfo? FindStylesField(Type concreteSheetType)
        {
            for (Type? t = concreteSheetType; t != null && t != typeof(object); t = t.BaseType)
            {
                var fi = t.GetField("styles", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null) return fi;
            }
            return null;
        }

        static IEnumerable<StyleModuleParameters> GetStyles(StyleSheetBase sheet)
        {
            var fi = FindStylesField(sheet.GetType());
            return fi?.GetValue(sheet) as IEnumerable<StyleModuleParameters>
                   ?? Enumerable.Empty<StyleModuleParameters>();
        }

        static IEnumerable<StyleSheetBase> GetSheets(ThemeAsset theme)
        {
            var fi = typeof(ThemeAsset)
                .GetField("styleSheets", BindingFlags.NonPublic | BindingFlags.Instance);
            return fi?.GetValue(theme) as IEnumerable<StyleSheetBase>
                   ?? Enumerable.Empty<StyleSheetBase>();
        }

        static ThemeAsset? GetBaseTheme(ThemeAsset t) =>
            typeof(ThemeAsset)
               .GetField("baseTheme", BindingFlags.NonPublic | BindingFlags.Instance)
               ?.GetValue(t) as ThemeAsset;

        // ───────────────────────────── green-dot logic ───────────────────────────
        bool IsOverride(string listName, string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            var baseTheme = GetBaseTheme((ThemeAsset)target);
            if (baseTheme == null) return false;

            var listField = typeof(ThemeAsset)
               .GetField(listName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (listField?.GetValue(baseTheme) is IEnumerable baseList)
            {
                foreach (var entry in baseList)
                {
                    var kf = entry.GetType().GetField("key");
                    if (kf != null && (string)kf.GetValue(entry) == key) return true;
                }
            }
            return false;
        }

        static SerializedProperty? FindValueProp(SerializedProperty element)
        {
            foreach (SerializedProperty child in element.Copy())
            {
                if (child.name is "color" or "sprite" or "font") return child;
            }
            return null;
        }
    }
}
