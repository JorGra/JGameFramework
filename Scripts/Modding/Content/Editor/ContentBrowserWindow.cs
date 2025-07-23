using JG.GameContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JG.GameContent.EditorTools
{
    public sealed class ContentBrowserWindow : EditorWindow
    {
        // ---------------- Menu ----------------
        [MenuItem("Tools/Content Browser %#c")]
        public static void ShowWindow()
        {
            var win = GetWindow<ContentBrowserWindow>();
            win.titleContent = new GUIContent("Content Browser");
            win.minSize = new Vector2(800, 400);
        }

        // ---------------- Fields --------------
        private Type[] _defTypes;
        private string[] _defTypeNames;
        private int _selectedTypeIndex;

        private Vector2 _scrollLeft, _scrollRight;
        private string _search = string.Empty;

        private IContentDef[] _currentDefs = Array.Empty<IContentDef>();
        private UnityEngine.Object _selectedObj;
        private Editor _cachedEditor;
        private SearchField _searchField;

        // ---------------- Lifetime ------------
        private void OnEnable()
        {
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += Repaint;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            RefreshTypeCache();
            RefreshCurrentDefs();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (_cachedEditor != null)
                DestroyImmediate(_cachedEditor);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Whenever we LEAVE play‑mode the runtime ScriptableObjects are gone.
            // Flush everything so the next repaint starts clean.
            if (state == PlayModeStateChange.EnteredEditMode ||
                state == PlayModeStateChange.ExitingPlayMode)
            {
                _selectedObj = null;
                _currentDefs = Array.Empty<IContentDef>();
                if (_cachedEditor != null)
                    DestroyImmediate(_cachedEditor);

                // Re‑populate catalogue view with fresh data (if any)
                RefreshCurrentDefs();
                Repaint();
            }
        }

        // ---------------- GUI -----------------
        private void OnGUI()
        {
            if (_defTypes == null || _defTypes.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No IContentDef types found.",
                    MessageType.Warning);
                return;
            }

            DrawToolbar();
            EditorGUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawListPane(position.width * 0.35f);
                DrawInspectorPane();
            }
        }

        // -------------- Toolbar ---------------
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int newIndex = GUILayout.Toolbar(
                    _selectedTypeIndex,
                    _defTypeNames,
                    EditorStyles.toolbarButton,
                    GUI.ToolbarButtonSize.FitToContents);

                if (newIndex != _selectedTypeIndex)
                {
                    _selectedTypeIndex = newIndex;
                    RefreshCurrentDefs();
                }

                GUILayout.FlexibleSpace();

                _search = _searchField.OnToolbarGUI(_search, GUILayout.Width(220));

                if (GUILayout.Button("Refresh",
                                     EditorStyles.toolbarButton,
                                     GUILayout.Width(70)))
                {
                    RefreshCurrentDefs();
                }
            }
        }

        // ------------- List pane --------------
        private void DrawListPane(float width)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                _scrollLeft = EditorGUILayout.BeginScrollView(_scrollLeft);

                foreach (var def in _currentDefs)
                {
                    // Skip objects that have been destroyed since the last repaint
                    if (def as UnityEngine.Object == null) continue;

                    if (!string.IsNullOrEmpty(_search) &&
                        !def.Id.Contains(_search, StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool wasSelected = _selectedObj == (def as UnityEngine.Object);
                    bool nowSelected = GUILayout.Toggle(wasSelected, def.Id, "Button");

                    if (nowSelected && !wasSelected)
                        SelectObject(def as UnityEngine.Object);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void SelectObject(UnityEngine.Object obj)
        {
            if (_selectedObj == obj) return;

            _selectedObj = obj;

            if (_cachedEditor != null)
                DestroyImmediate(_cachedEditor);

            if (_selectedObj != null)
                _cachedEditor = Editor.CreateEditor(_selectedObj);
        }

        // ----------- Inspector pane -----------
        private void DrawInspectorPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                _scrollRight = EditorGUILayout.BeginScrollView(_scrollRight);

                if (_cachedEditor != null &&
                    _selectedObj != null) // guard against destroyed targets
                {
                    _cachedEditor.OnInspectorGUI();
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Select an entry on the left to see its details.",
                        MessageType.Info);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        // ---------- Data refresh --------------
        private void RefreshTypeCache()
        {
            _defTypes = Assembly.GetAssembly(typeof(IContentDef))
                                .GetTypes()
                                .Where(t => typeof(IContentDef).IsAssignableFrom(t) && !t.IsAbstract)
                                .OrderBy(t => t.Name)
                                .ToArray();

            _defTypeNames = _defTypes
                .Select(t => ObjectNames.NicifyVariableName(t.Name))
                .ToArray();
        }

        private void RefreshCurrentDefs()
        {
            if (_defTypes == null || _defTypes.Length == 0) return;

            _selectedTypeIndex = Mathf.Clamp(_selectedTypeIndex, 0, _defTypes.Length - 1);

            var concreteType = _defTypes[_selectedTypeIndex];
            var method = typeof(ContentCatalogue)
                         .GetMethod(nameof(ContentCatalogue.GetAll))
                         .MakeGenericMethod(concreteType);

            _currentDefs = ((IEnumerable<IContentDef>)method.Invoke(ContentCatalogue.Instance, null))
                           .Where(d => d as UnityEngine.Object != null)     // strip destroyed refs
                           .OrderBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
                           .ToArray();

            SelectObject(null);        // clears _cachedEditor safely
        }
    }
}
