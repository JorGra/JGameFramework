using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JG.GameContent;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace JG.GameContent.EditorTools
{
    [CustomPropertyDrawer(typeof(IdReferenceAttribute))]
    public sealed class IdReferenceDrawer : PropertyDrawer
    {
        private const float StatusSize = 12f;
        private const float ButtonWidth = 56f;
        private const float Spacing = 3f;

        private static readonly Dictionary<string, ReorderableList> _lists = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (IdReferenceAttribute)attribute;

            // Handle lists/arrays of strings
            if (property.isArray && property.arrayElementType == "string")
            {
                string key = property.serializedObject.targetObject.GetInstanceID() + "/" + property.propertyPath;
                if (!_lists.TryGetValue(key, out var list) || list == null || list.serializedProperty != property)
                {
                    list = new ReorderableList(property.serializedObject, property, true, true, true, true);
                    list.drawHeaderCallback = r =>
                    {
                        var title = string.IsNullOrEmpty(label.text) ? property.displayName : label.text;
                        var typeName = attr.TargetType != null ? attr.TargetType.Name : "ContentDef";
                        EditorGUI.LabelField(r, $"{title} ({typeName} Ids)");
                    };
                    list.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
                    list.drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        rect.height = EditorGUIUtility.singleLineHeight;
                        rect.y += 2f;
                        var elem = property.GetArrayElementAtIndex(index);
                        DrawStringWithHelpers(rect, GUIContent.none, elem, attr);
                    };
                    _lists[key] = list;
                }
                _lists[key].DoList(position);
                return;
            }

            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            DrawStringWithHelpers(position, label, property, attr);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (IdReferenceAttribute)attribute;

            if (property.isArray && property.arrayElementType == "string")
            {
                string key = property.serializedObject.targetObject.GetInstanceID() + "/" + property.propertyPath;
                if (!_lists.TryGetValue(key, out var list) || list == null || list.serializedProperty != property)
                {
                    list = new ReorderableList(property.serializedObject, property, true, true, true, true);
                    list.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
                    list.drawHeaderCallback = r =>
                    {
                        var title = string.IsNullOrEmpty(label.text) ? property.displayName : label.text;
                        var typeName = attr.TargetType != null ? attr.TargetType.Name : "ContentDef";
                        EditorGUI.LabelField(r, $"{title} ({typeName} Ids)");
                    };
                    list.drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        rect.height = EditorGUIUtility.singleLineHeight;
                        rect.y += 2f;
                        var elem = property.GetArrayElementAtIndex(index);
                        DrawStringWithHelpers(rect, GUIContent.none, elem, attr);
                    };
                    _lists[key] = list;
                }
                return _lists[key].GetHeight();
            }

            return property.propertyType == SerializedPropertyType.String
                ? EditorGUIUtility.singleLineHeight
                : EditorGUI.GetPropertyHeight(property, label, true);
        }

        private void DrawStringWithHelpers(Rect position, GUIContent label, SerializedProperty stringProp, IdReferenceAttribute attr)
        {
            string id = stringProp.stringValue;

            UnityEngine.Object targetObj = null;
            bool found = false;
            if (!string.IsNullOrWhiteSpace(id) && attr.TargetType != null)
            {
                try
                {
                    var m = typeof(ContentCatalogue).GetMethod(nameof(ContentCatalogue.GetAll)).MakeGenericMethod(attr.TargetType);
                    var seq = (IEnumerable)m.Invoke(ContentCatalogue.Instance, null);
                    foreach (var o in seq)
                    {
                        if (o is IContentDef def && def is UnityEngine.Object uo)
                        {
                            if (string.Equals(def.Id, id, StringComparison.OrdinalIgnoreCase))
                            {
                                targetObj = uo;
                                found = targetObj != null;
                                break;
                            }
                        }
                    }
                }
                catch { }
            }

            var line = position;
            line.height = EditorGUIUtility.singleLineHeight;

            float rightWidth = StatusSize + Spacing + ButtonWidth + Spacing + ButtonWidth;
            float labelWidth = (label != GUIContent.none && !string.IsNullOrEmpty(label.text)) ? EditorGUIUtility.labelWidth : 0f;
            var textRect = new Rect(line.x, line.y, Mathf.Max(0, line.width - rightWidth - Spacing), line.height);

            if (labelWidth > 0f)
            {
                var labelRect = new Rect(textRect.x, line.y, labelWidth, line.height);
                EditorGUI.LabelField(labelRect, label);
                textRect.xMin = labelRect.xMax + 2f;
            }

            var statusRect = new Rect(textRect.xMax + Spacing, line.y + (line.height - StatusSize) * 0.5f, StatusSize, StatusSize);
            var openRect = new Rect(statusRect.xMax + Spacing, line.y, ButtonWidth, line.height);
            var inlineRect = new Rect(openRect.xMax + Spacing, line.y, ButtonWidth, line.height);

            EditorGUI.BeginChangeCheck();
            var newVal = EditorGUI.TextField(textRect, id);
            if (EditorGUI.EndChangeCheck())
                stringProp.stringValue = newVal;

            // Status dot (green=ok, red=missing, gray=empty/optional)
            var col = found ? new Color(0.25f, 0.8f, 0.25f)
                            : (string.IsNullOrWhiteSpace(id) ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.9f, 0.35f, 0.25f));
            var prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(statusRect, EditorGUIUtility.whiteTexture);
            GUI.color = prev;
            var tip = found ? $"Found {targetObj?.GetType().Name}"
                            : (string.IsNullOrWhiteSpace(id) ? (attr.Optional ? "Optional (empty)" : "Empty") : "Missing");
            EditorGUI.LabelField(statusRect, new GUIContent(string.Empty, tip));

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(stringProp.stringValue)))
            {
                if (GUI.Button(openRect, new GUIContent("Open", "Open in Content Browser")))
                {
                    var targetType = attr.TargetType;
                    var idValue = stringProp.stringValue; // capture
                    EditorApplication.delayCall += () =>
                    {
                        if (!ContentBrowserAPI.FocusAndSelectById(targetType, idValue))
                            ContentBrowserAPI.FocusAndSearch(idValue, targetType);
                    };
                    GUIUtility.ExitGUI();
                }
            }
            using (new EditorGUI.DisabledScope(!found || targetObj == null))
            {
                if (GUI.Button(inlineRect, new GUIContent("Inline", "Open floating inspector to edit referenced def")))
                {
                    var obj = targetObj; // capture
                    EditorApplication.delayCall += () => InlineDefInspectorWindow.Show(obj);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
