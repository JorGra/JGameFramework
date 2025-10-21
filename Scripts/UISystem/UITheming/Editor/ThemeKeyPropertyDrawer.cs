using System;
using System.Collections.Generic;
using TMPro;
using UI.Tools.Editor.CustomPropertyDrawers;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UI.Theming.Editor
{
    [CustomPropertyDrawer(typeof(ThemeKeyAttribute))]
    public sealed class ThemeKeyPropertyDrawer : PropertyDrawer
    {
        const float buttonWidth = 22f;
        const float applyWidth = 22f;
        const float previewWidth = 20f;
        const float gap = 2f;
        static readonly GUIContent pickContent = new GUIContent("...", "Pick a themed key");
        static readonly GUIContent applyContent;
        static readonly GUIContent refreshContent;
        static readonly Color invalidOverlay = new Color(0.9f, 0.2f, 0.2f, 0.2f);
        static readonly GUIStyle fontPreviewStyle;

        static ThemeKeyPropertyDrawer()
        {
            fontPreviewStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

            refreshContent = EditorGUIUtility.IconContent("Refresh");
            if (refreshContent.image == null && string.IsNullOrEmpty(refreshContent.text))
            {
                refreshContent = new GUIContent("R", "Refresh theme keys");
            }
            else
            {
                refreshContent.tooltip = "Refresh theme keys";
            }

            applyContent = EditorGUIUtility.IconContent("SceneViewFx");
            if (applyContent.image == null && string.IsNullOrEmpty(applyContent.text))
            {
                applyContent = new GUIContent("Go");
            }
            applyContent.tooltip = "Apply the selected theme to this element";
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!ToggleGroupEditorUtility.ShouldDisplay(property, fieldInfo))
            {
                return;
            }

            ThemeKeyAttribute attr = (ThemeKeyAttribute)attribute;
            EditorGUI.BeginProperty(position, label, property);

            Rect fieldRect = EditorGUI.PrefixLabel(position, label);
            bool showPreview = RequiresPreview(attr.Kind);
            bool canApply = HasThemeableTarget(property);

            float reservedWidth = gap + buttonWidth;
            if (canApply)
            {
                reservedWidth += applyWidth + gap;
            }
            if (showPreview)
            {
                reservedWidth += previewWidth + gap;
            }

            float textWidth = Mathf.Max(20f, fieldRect.width - reservedWidth);
            var textRect = new Rect(fieldRect.x, fieldRect.y, textWidth, EditorGUIUtility.singleLineHeight);
            float cursor = textRect.xMax + gap;

            var previewRect = showPreview
                ? new Rect(cursor, fieldRect.y + 2f, previewWidth, EditorGUIUtility.singleLineHeight - 4f)
                : Rect.zero;

            if (showPreview)
            {
                cursor = previewRect.xMax + gap;
            }

            var applyRect = canApply
                ? new Rect(cursor, fieldRect.y, applyWidth, EditorGUIUtility.singleLineHeight)
                : Rect.zero;

            if (canApply)
            {
                cursor = applyRect.xMax + gap;
            }

            var buttonRect = new Rect(cursor, fieldRect.y, buttonWidth, EditorGUIUtility.singleLineHeight);

            List<ThemeKeyInfo> keys = ThemeKeyPickerUtility.GetKeys(attr.Kind, attr.StyleType);
            var selectedInfo = ThemeKeyPickerUtility.Find(keys, property.stringValue);
            bool hasValue = selectedInfo.HasValue;
            ThemeKeyInfo info = selectedInfo.GetValueOrDefault();

            if (!hasValue && !string.IsNullOrEmpty(property.stringValue))
            {
                EditorGUI.DrawRect(new Rect(textRect.x - 1f, textRect.y, textRect.width + 2f, textRect.height), invalidOverlay);
            }

            EditorGUI.BeginChangeCheck();
            string newValue = EditorGUI.TextField(textRect, property.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = newValue;
                selectedInfo = ThemeKeyPickerUtility.Find(keys, property.stringValue);
                hasValue = selectedInfo.HasValue;
                info = selectedInfo.GetValueOrDefault();
            }

            bool drawPreview = showPreview && hasValue;

            if (drawPreview && hasValue)
            {
                DrawPreview(previewRect, attr.Kind, info);
            }

            if (canApply)
            {
                bool enableApply = hasValue && info.Theme != null;
                using (new EditorGUI.DisabledScope(!enableApply))
                {
                    if (GUI.Button(applyRect, applyContent, EditorStyles.miniButton))
                    {
                        ApplyThemeToTargets(property, info);
                    }
                }
            }

            if (GUI.Button(buttonRect, pickContent, EditorStyles.miniButton))
            {
                var screenRect = GUIUtility.GUIToScreenRect(buttonRect);
                var popup = new ThemeKeySearchPopup(attr.Kind, attr.StyleType, keys, property.serializedObject, property.propertyPath);
                PopupWindow.Show(screenRect, popup);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ToggleGroupEditorUtility.ShouldDisplay(property, fieldInfo)
                ? EditorGUIUtility.singleLineHeight
                : 0f;
        }

        static bool HasThemeableTarget(SerializedProperty property)
        {
            var serializedObject = property?.serializedObject;
            var targets = serializedObject?.targetObjects;
            if (targets == null || targets.Length == 0) return false;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is IThemeable)
                {
                    return true;
                }
            }
            return false;
        }

        static void ApplyThemeToTargets(SerializedProperty property, ThemeKeyInfo info)
        {
            ThemeAsset theme = info.Theme;
            if (theme == null) return;

            var serializedObject = property.serializedObject;
            var targets = serializedObject?.targetObjects;
            if (targets == null || targets.Length == 0) return;

            var toRecord = new List<UnityEngine.Object>(targets.Length);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is IThemeable && targets[i] != null)
                {
                    toRecord.Add(targets[i]);
                }
            }

            if (toRecord.Count == 0) return;

            Undo.RecordObjects(toRecord.ToArray(), "Apply Theme");
            foreach (var obj in toRecord)
            {
                if (obj is IThemeable themeable)
                {
                    themeable.ApplyTheme(theme);
                    EditorUtility.SetDirty(obj);
                    if (PrefabUtility.IsPartOfPrefabInstance(obj))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                    }
                }
            }
        }

        internal static bool RequiresPreview(ThemeKeyKind kind) => kind == ThemeKeyKind.Color || kind == ThemeKeyKind.Sprite || kind == ThemeKeyKind.Font;

        internal static GUIContent RefreshContent => refreshContent;

        internal static void DrawPreview(Rect rect, ThemeKeyKind kind, ThemeKeyInfo info)
        {
            switch (kind)
            {
                case ThemeKeyKind.Color when info.Color.HasValue:
                    EditorGUI.DrawRect(rect, info.Color.Value);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), Color.black);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Color.black);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), Color.black);
                    EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), Color.black);
                    break;
                case ThemeKeyKind.Sprite when info.Sprite:
                    var texture = AssetPreview.GetAssetPreview(info.Sprite) ?? AssetPreview.GetMiniThumbnail(info.Sprite);
                    if (texture)
                        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
                    break;
                case ThemeKeyKind.Font when info.Font:
                    fontPreviewStyle.font = info.Font ? info.Font.sourceFontFile : null;
                    GUI.Label(rect, "Ag", fontPreviewStyle);
                    break;
            }
        }
    }

    sealed class ThemeKeySearchPopup : PopupWindowContent
    {
        readonly ThemeKeyKind kind;
        readonly Type styleType;
        List<ThemeKeyInfo> keys;
        readonly SerializedObject serializedObject;
        readonly string propertyPath;
        readonly SearchField searchField = new();
        string search = string.Empty;
        Vector2 scroll;

        public ThemeKeySearchPopup(ThemeKeyKind kind, Type styleType, List<ThemeKeyInfo> keys,
            SerializedObject serializedObject, string propertyPath)
        {
            this.kind = kind;
            this.styleType = styleType;
            this.keys = keys;
            this.serializedObject = serializedObject;
            this.propertyPath = propertyPath;
        }

        public override Vector2 GetWindowSize() => new Vector2(320f, 360f);

        public override void OnGUI(Rect rect)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(GetHeaderLabel(), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                search = searchField.OnToolbarGUI(search);
                if (GUILayout.Button(ThemeKeyPropertyDrawer.RefreshContent, EditorStyles.toolbarButton, GUILayout.Width(24f)))
                {
                    Refresh();
                    GUI.FocusControl(null);
                }
            }

            if (keys.Count == 0)
            {
                EditorGUILayout.HelpBox("No keys were found in ThemeAssets.", MessageType.Info);
                if (GUILayout.Button("Refresh"))
                {
                    Refresh();
                }
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var info in keys)
            {
                if (!PassesSearch(info.Key, info.SourceName))
                    continue;

                DrawRow(info);
            }
            EditorGUILayout.EndScrollView();
        }

        string GetHeaderLabel()
        {
            return kind switch
            {
                ThemeKeyKind.Color => "Colour Keys",
                ThemeKeyKind.Sprite => "Sprite Keys",
                ThemeKeyKind.Font => "Font Keys",
                ThemeKeyKind.Style => styleType != null ? $"{styleType.Name} Keys" : "Style Keys",
                _ => "Theme Keys"
            };
        }

        bool PassesSearch(string key, string source)
        {
            if (string.IsNullOrEmpty(search)) return true;
            var comparison = StringComparison.OrdinalIgnoreCase;
            return key.IndexOf(search, comparison) >= 0 || source.IndexOf(search, comparison) >= 0;
        }

        void DrawRow(ThemeKeyInfo info)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                Apply(info.Key);
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            var previewRect = new Rect(rowRect.x, rowRect.y + 2f, 18f, rowRect.height - 4f);
            var keyRect = new Rect(previewRect.xMax + 4f, rowRect.y, rowRect.width - 140f, rowRect.height);
            var sourceRect = new Rect(rowRect.xMax - 120f, rowRect.y, 120f, rowRect.height);

            if (ThemeKeyPropertyDrawer.RequiresPreview(kind))
            {
                ThemeKeyPropertyDrawer.DrawPreview(previewRect, kind, info);
            }

            EditorGUI.LabelField(keyRect, info.Key);

            string sourceLabel = info.SourceName;
            if (kind == ThemeKeyKind.Style && info.StyleType != null)
            {
                sourceLabel = $"{info.SourceName} - {info.StyleType.Name}";
            }
            EditorGUI.LabelField(sourceRect, sourceLabel, EditorStyles.miniLabel);
        }

        void Apply(string value)
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty(propertyPath);
            if (property != null)
            {
                property.stringValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }

        void Refresh()
        {
            ThemeKeyPickerUtility.ClearCache();
            keys = ThemeKeyPickerUtility.GetKeys(kind, styleType);
            editorWindow?.Repaint();
        }
    }
}
