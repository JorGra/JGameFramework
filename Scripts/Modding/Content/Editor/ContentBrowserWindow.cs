using JG.GameContent;
using JG.Modding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Editor = UnityEditor.Editor;
using JG.Inventory; // for IInventoryItem & ItemEffectDefinition

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
        // ItemType caches / hierarchy
        private Type[] _allAssignableTypes = Array.Empty<Type>();
        private Type[] _leafDefTypes = Array.Empty<Type>();
        private Dictionary<Type, List<Type>> _childrenByType = new();
        private List<Type> _rootTypes = new();
        private Dictionary<int, Type> _treeIdToType = new();

        // UI state
        private Vector2 _scrollTypes, _scrollList, _scrollInspector;
        private string _search = string.Empty;
        private bool _includeDerived = true;
        private int _selectedTypeTreeId = 0; // 0 == All
        private float _leftWidth = 260f;
        private float _middleWidth = 440f;
        private bool _inspectorBottom = false;
        private float _bottomHeight = 260f;
        private bool _draggingSplit = false;
        private bool _draggingLeftSplit = false;
        private bool _draggingRightSplit = false;
        private Vector2 _splitDragStartMouse;
        private float _splitStartLeftWidth;
        private float _splitStartMiddleWidth;

        // Data state
        private IContentDef[] _currentDefs = Array.Empty<IContentDef>();
        private UnityEngine.Object _selectedObj;
        private UnityEditor.Editor _cachedEditor;

        // Helpers
        private SearchField _searchField;
        private GUIStyle _listHeaderStyle;
        private GUIStyle _leftAlignedButtonStyle;
        private bool _dirtySelection;
        private bool _effectsFoldout;

        // ---------------- Lifetime ------------
        private void OnEnable()
        {
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += Repaint;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            BuildTypeHierarchy();
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
            if (_leafDefTypes == null || _leafDefTypes.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No IContentDef types found.",
                    MessageType.Warning);
                return;
            }

            DrawToolbar();
            EditorGUILayout.Space(2f);

            if (_inspectorBottom)
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                    {
                        DrawTypePane();
                        DrawLeftSplitter();
                        DrawListPane();
                    }

                    // Splitter handle
                    var splitRect = GUILayoutUtility.GetRect(1, 4, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(splitRect, new Color(0, 0, 0, 0.2f));
                    EditorGUIUtility.AddCursorRect(splitRect, MouseCursor.ResizeVertical);
                    if (Event.current.type == EventType.MouseDown && splitRect.Contains(Event.current.mousePosition))
                    {
                        _draggingSplit = true;
                        Event.current.Use();
                    }
                    if (_draggingSplit && Event.current.type == EventType.MouseDrag)
                    {
                        _bottomHeight = Mathf.Clamp(_bottomHeight - Event.current.delta.y, 160f, position.height - 220f);
                        Repaint();
                        Event.current.Use();
                    }
                    if (Event.current.type == EventType.MouseUp && _draggingSplit)
                    {
                        _draggingSplit = false;
                        Event.current.Use();
                    }

                    using (new EditorGUILayout.VerticalScope(GUILayout.Height(Mathf.Clamp(_bottomHeight, 160f, position.height - 220f))))
                    {
                        DrawInspectorPane();
                    }
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    DrawTypePane();
                    DrawLeftSplitter();
                    DrawListPane();
                    DrawRightSplitter();
                    DrawInspectorPane();
                }
            }
        }

        // -------------- Toolbar ---------------
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Include derived toggle impacts filtering
                bool includeDerived = GUILayout.Toggle(_includeDerived, "Include derived", EditorStyles.toolbarButton, GUILayout.Width(120));
                if (includeDerived != _includeDerived)
                {
                    _includeDerived = includeDerived;
                    RefreshCurrentDefs();
                }

                _inspectorBottom = GUILayout.Toggle(_inspectorBottom, "Inspector bottom", EditorStyles.toolbarButton, GUILayout.Width(130));

                GUILayout.FlexibleSpace();

                // Make the search field flex, but ensure buttons on the right remain visible
                float reserved = 100 + 70 + 12; // Reload + Refresh + small padding
                reserved += 120 + 130 + 24;     // toggles + padding
                float searchWidth = Mathf.Max(120f, position.width - reserved);
                _search = _searchField.OnToolbarGUI(_search, GUILayout.Width(searchWidth));

                if (GUILayout.Button("Reload Mods", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    ReloadModsInEditor();
                }
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    RefreshCurrentDefs();
                }
            }
        }

        private void DrawLeftSplitter()
        {
            var rect = GUILayoutUtility.GetRect(4, 1, GUILayout.Width(4), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _draggingLeftSplit = true;
                _splitDragStartMouse = Event.current.mousePosition;
                _splitStartLeftWidth = _leftWidth;
                Event.current.Use();
            }
            if (_draggingLeftSplit && Event.current.type == EventType.MouseDrag)
            {
                float delta = Event.current.mousePosition.x - _splitDragStartMouse.x;
                float minLeft = 160f;
                float maxLeft = _inspectorBottom ? (position.width - 300f) : (position.width - _middleWidth - 220f);
                _leftWidth = Mathf.Clamp(_splitStartLeftWidth + delta, minLeft, Mathf.Max(minLeft, maxLeft));
                Repaint();
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseUp && _draggingLeftSplit)
            {
                _draggingLeftSplit = false;
                Event.current.Use();
            }
        }

        private void DrawRightSplitter()
        {
            var rect = GUILayoutUtility.GetRect(4, 1, GUILayout.Width(4), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _draggingRightSplit = true;
                _splitDragStartMouse = Event.current.mousePosition;
                _splitStartMiddleWidth = _middleWidth;
                Event.current.Use();
            }
            if (_draggingRightSplit && Event.current.type == EventType.MouseDrag)
            {
                float delta = Event.current.mousePosition.x - _splitDragStartMouse.x;
                float minMiddle = 280f;
                float maxMiddle = position.width - _leftWidth - 220f; // leave room for inspector
                _middleWidth = Mathf.Clamp(_splitStartMiddleWidth + delta, minMiddle, Mathf.Max(minMiddle, maxMiddle));
                Repaint();
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseUp && _draggingRightSplit)
            {
                _draggingRightSplit = false;
                Event.current.Use();
            }
        }

        // ------------- Types pane --------------
        private void DrawTypePane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(_leftWidth), GUILayout.ExpandHeight(true)))
            {
                GUILayout.Label("Content Types", EditorStyles.boldLabel);
                _scrollTypes = EditorGUILayout.BeginScrollView(_scrollTypes, GUILayout.ExpandHeight(true));

                // Root node
                DrawTypeNode(0, "All", 0);

                // Top-level roots
                foreach (var t in _rootTypes)
                {
                    DrawTypeSubtree(t, 0);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawTypeNode(int id, string label, int indent)
        {
            var was = _selectedTypeTreeId == id;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(indent * 14f);
                if (_leftAlignedButtonStyle == null)
                {
                    _leftAlignedButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        fixedHeight = 22f,
                        wordWrap = false
                    };
                    _leftAlignedButtonStyle.padding = new RectOffset(8, 8, 2, 2);
                }
                var selected = GUILayout.Toggle(was, label, _leftAlignedButtonStyle);
                if (selected && !was)
                {
                    _selectedTypeTreeId = id;
                    RefreshCurrentDefs();
                }
            }
        }

        private void DrawTypeSubtree(Type t, int indent)
        {
            int id = GetTreeIdForType(t);
            DrawTypeNode(id, ObjectNames.NicifyVariableName(t.Name), indent);
            if (_childrenByType.TryGetValue(t, out var children))
            {
                foreach (var c in children)
                    DrawTypeSubtree(c, indent + 1);
            }
        }

        private int GetTreeIdForType(Type t)
        {
            foreach (var kv in _treeIdToType)
                if (kv.Value == t) return kv.Key;
            int id = _treeIdToType.Count + 1; // 0 is All
            _treeIdToType[id] = t;
            return id;
        }

        private Type GetSelectedFilterType()
        {
            if (_selectedTypeTreeId == 0) return null; // All
            return _treeIdToType.TryGetValue(_selectedTypeTreeId, out var t) ? t : null;
        }

        // ------------- List pane --------------
        private void DrawListPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(_middleWidth), GUILayout.ExpandHeight(true)))
            {
                if (_listHeaderStyle == null)
                {
                    _listHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label("Id", _listHeaderStyle, GUILayout.Width(Mathf.Max(180f, _middleWidth * 0.45f)));
                    GUILayout.Label("Type", _listHeaderStyle, GUILayout.Width(Mathf.Max(80f, _middleWidth * 0.22f)));
                    GUILayout.Label("File", _listHeaderStyle);
                }

                _scrollList = EditorGUILayout.BeginScrollView(_scrollList, GUILayout.ExpandHeight(true));
                foreach (var def in _currentDefs)
                {
                    if (def as UnityEngine.Object == null) continue;
                    if (!string.IsNullOrEmpty(_search) && !def.Id.Contains(_search, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var obj = def as UnityEngine.Object;
                    bool wasSelected = _selectedObj == obj;

                    // Create a full-width row rect and make the whole row clickable
                    var rowRect = GUILayoutUtility.GetRect(10, 22, GUILayout.ExpandWidth(true));
                    if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
                    {
                        SelectObject(obj);
                        Event.current.Use();
                    }

                    // Draw background for selection
                    if (wasSelected)
                        EditorGUI.DrawRect(rowRect, new Color(0.24f, 0.48f, 0.90f, 0.18f));

                    // Split columns
                    var idWidth = Mathf.Max(180f, _middleWidth * 0.45f);
                    var typeWidth = Mathf.Max(80f, _middleWidth * 0.22f);
                    var idRect = new Rect(rowRect.x, rowRect.y, idWidth, rowRect.height);
                    var typeRect = new Rect(idRect.xMax + 4, rowRect.y, typeWidth, rowRect.height);
                    var fileRect = new Rect(typeRect.xMax + 4, rowRect.y, rowRect.xMax - (typeRect.xMax + 4), rowRect.height);

                    GUI.Label(idRect, def.Id, EditorStyles.label);
                    GUI.Label(typeRect, obj.GetType().Name, EditorStyles.miniLabel);
                    GUI.Label(fileRect, Path.GetFileName((def.SourceFile ?? "").Replace("\\", "/")), EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void SelectObject(UnityEngine.Object obj)
        {
            if (_selectedObj == obj) return;

            _selectedObj = obj;
            _dirtySelection = false;

            if (_cachedEditor != null)
                DestroyImmediate(_cachedEditor);

            if (_selectedObj != null)
                _cachedEditor = UnityEditor.Editor.CreateEditor(_selectedObj);

            Repaint();
        }

        // ----------- Inspector pane -----------
        private void DrawInspectorPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
            {
                // Inspector toolbar
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    EditorGUI.BeginDisabledGroup(!(_selectedObj is IContentDef));
                    if (GUILayout.Button("Save to JSON", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    {
                        TrySaveSelectionToJson();
                    }
                    if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    {
                        DuplicateSelection();
                    }
                    if (GUILayout.Button("Open JSON", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    {
                        OpenJsonForSelection();
                    }
                    if (GUILayout.Button("Reveal", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    {
                        RevealJsonForSelection();
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUI.EndDisabledGroup();
                }

                _scrollInspector = EditorGUILayout.BeginScrollView(_scrollInspector, GUILayout.ExpandHeight(true));

                if (_cachedEditor != null && _selectedObj != null)
                {
                    EditorGUI.BeginChangeCheck();
                    _cachedEditor.OnInspectorGUI();
                    if (EditorGUI.EndChangeCheck())
                    {
                        _dirtySelection = true;
                    }

                    // Render item effects (read-only) if this def has them
                    if (_selectedObj is IInventoryItem invItem && invItem.Effects != null)
                    {
                        EditorGUILayout.Space(6);
                        _effectsFoldout = EditorGUILayout.Foldout(_effectsFoldout, "Item Effects", true);
                        if (_effectsFoldout)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                int idx = 0;
                                foreach (var eff in invItem.Effects)
                                {
                                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                                    {
                                        EditorGUILayout.LabelField($"#{idx} Type", eff?.effectType ?? "(null)", EditorStyles.boldLabel);
                                        if (eff != null && eff.effectParams != null)
                                        {
                                            // Pretty-print the JToken params
                                            string json = eff.effectParams.ToString(Newtonsoft.Json.Formatting.Indented);
                                            EditorGUILayout.TextArea(json, GUILayout.MinHeight(44));
                                        }
                                        else
                                        {
                                            EditorGUILayout.LabelField("Params", "(null)");
                                        }
                                    }
                                    idx++;
                                }
                                if (idx == 0)
                                {
                                    EditorGUILayout.LabelField("(no effects)");
                                }
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Select an entry to edit and save back to its JSON.",
                        MessageType.Info);
                }

                EditorGUILayout.EndScrollView();

                // Dirty indicator
                if (_selectedObj != null && _dirtySelection)
                {
                    EditorGUILayout.HelpBox("Unsaved changes.", MessageType.Warning);
                }
            }
        }

        // ---------- ItemType hierarchy --------------
        private void BuildTypeHierarchy()
        {
            var asm = Assembly.GetAssembly(typeof(IContentDef));
            var all = asm.GetTypes()
                         .Where(t => t.IsClass && typeof(IContentDef).IsAssignableFrom(t) && t != typeof(IContentDef))
                         .OrderBy(t => t.Name)
                         .ToArray();
            _allAssignableTypes = all;
            _leafDefTypes = all.Where(t => !t.IsAbstract).ToArray();

            _childrenByType.Clear();
            _rootTypes.Clear();
            _treeIdToType.Clear();

            // Build parent -> children map; roots under null
            foreach (var t in all)
            {
                var parent = t.BaseType;
                if (parent == null || !typeof(IContentDef).IsAssignableFrom(parent))
                {
                    // root
                    _rootTypes.Add(t);
                }
                else
                {
                    if (!_childrenByType.TryGetValue(parent, out var list))
                        _childrenByType[parent] = list = new List<Type>();
                    list.Add(t);
                }
            }

            // Sort children lists by nicified name for stable UI
            _rootTypes = _rootTypes.Distinct().OrderBy(x => ObjectNames.NicifyVariableName(x.Name)).ToList();
            foreach (var kv in _childrenByType.ToList())
                _childrenByType[kv.Key] = kv.Value.Distinct().OrderBy(x => ObjectNames.NicifyVariableName(x.Name)).ToList();
        }

        private void RefreshCurrentDefs()
        {
            // Determine selected filter type
            var filterType = GetSelectedFilterType();

            IEnumerable<IContentDef> all;
            if (filterType == null)
            {
                // All content: flatten across all leaf types
                var list = new List<IContentDef>();
                foreach (var t in _leafDefTypes)
                {
                    var m = typeof(ContentCatalogue).GetMethod(nameof(ContentCatalogue.GetAll)).MakeGenericMethod(t);
                    var seq = (IEnumerable<IContentDef>)m.Invoke(ContentCatalogue.Instance, null);
                    list.AddRange(seq);
                }
                all = list;
            }
            else
            {
                var m = typeof(ContentCatalogue).GetMethod(nameof(ContentCatalogue.GetAll)).MakeGenericMethod(filterType);
                all = (IEnumerable<IContentDef>)m.Invoke(ContentCatalogue.Instance, null);
                if (!_includeDerived)
                {
                    all = all.Where(d => (d as UnityEngine.Object)?.GetType() == filterType);
                }
            }

            _currentDefs = (all ?? Enumerable.Empty<IContentDef>())
                           .Where(d => d as UnityEngine.Object != null)
                           .OrderBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
                           .ToArray();

            SelectObject(null);
        }

        // ---------- JSON save / utilities ----------
        private static readonly JsonSerializer _writer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new UnityIgnoreResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        private void TrySaveSelectionToJson()
        {
            if (!(_selectedObj is IContentDef def) || def == null) return;

            var src = def.SourceFile;
            if (string.IsNullOrWhiteSpace(src) || !File.Exists(src))
            {
                var suggestedName = SanitizeFileName(def.Id) + ".json";
                var initialDir = GuessInitialSaveDirectory(def.GetType());
                var path = EditorUtility.SaveFilePanel("Save Content JSON", initialDir, suggestedName, "json");
                if (string.IsNullOrEmpty(path)) return;
                src = path;
                def.SourceFile = src;
            }

            try
            {
                JToken token = null;
                if (File.Exists(src))
                {
                    var text = File.ReadAllText(src);
                    if (!string.IsNullOrWhiteSpace(text))
                        token = JToken.Parse(text);
                }

                var newObj = SerializeDefToJObject(def);

                if (token == null)
                {
                    // New file: write single object
                    WriteJsonToFile(src, newObj);
                }
                else if (token.Type == JTokenType.Array)
                {
                    var arr = (JArray)token;
                    var idx = IndexOfById(arr, def.Id);
                    if (idx >= 0)
                        arr[idx] = newObj;
                    else
                        arr.Add(newObj);
                    WriteJsonToFile(src, arr);
                }
                else if (token.Type == JTokenType.Object)
                {
                    // Replace entire object
                    WriteJsonToFile(src, newObj);
                }
                else
                {
                    throw new Exception($"Unsupported JSON root in {src}: {token.Type}");
                }

                _dirtySelection = false;
                ShowNotification(new GUIContent("Saved JSON ✔"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed saving JSON for {def.Id}: {ex.Message}\n{ex}");
                EditorUtility.DisplayDialog("Save Failed", ex.Message, "OK");
            }
        }

        private void OpenJsonForSelection()
        {
            if (!(_selectedObj is IContentDef def) || string.IsNullOrWhiteSpace(def.SourceFile)) return;
            if (File.Exists(def.SourceFile))
                EditorUtility.OpenWithDefaultApp(def.SourceFile);
        }

        private void RevealJsonForSelection()
        {
            if (!(_selectedObj is IContentDef def) || string.IsNullOrWhiteSpace(def.SourceFile)) return;
            if (File.Exists(def.SourceFile))
                EditorUtility.RevealInFinder(def.SourceFile);
        }

        private static void WriteJsonToFile(string path, JToken token)
        {
            var json = ToPrettyString(token);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }

        private static string ToPrettyString(JToken token)
        {
            using var sw = new StringWriter();
            using var jw = new JsonTextWriter(sw) { Formatting = Formatting.Indented, Indentation = 2 };
            token.WriteTo(jw);
            return sw.ToString();
        }

        private static JObject SerializeDefToJObject(IContentDef def)
        {
            // Start with full object then prune UnityEngine.Object, ScriptableObject fluff, and metadata
            var jo = JObject.FromObject(def, _writer);

            // Remove fields we don't want to persist
            jo.Remove("name");
            jo.Remove("SourceFile");

            // Remove any UnityEngine.Object-typed members by reflecting the type
            var t = def.GetType();
            var unityNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var n in GetUnityMemberNames(t))
                unityNames.Add(n);
            foreach (var n in unityNames)
            {
                jo.Remove(n);
            }

            // Ensure Id is present and consistent (prefer lower-case key if present)
            var idKey = FindExistingIdKey(jo) ?? "Id";
            jo[idKey] = def.Id;

            return jo;
        }

        private static string FindExistingIdKey(JObject jo)
        {
            foreach (var p in jo.Properties())
                if (string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase))
                    return p.Name;
            return null;
        }

        private static IEnumerable<string> GetUnityMemberNames(Type t)
        {
            // Public fields
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
                    yield return f.Name;

            // Public properties with setters
            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!p.CanRead) continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(p.PropertyType))
                    yield return p.Name;
            }
        }

        private static int IndexOfById(JArray arr, string id)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i] is JObject o)
                {
                    var val = GetIdCaseInsensitive(o);
                    if (val != null && string.Equals(val, id, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }
            return -1;
        }

        private static string GetIdCaseInsensitive(JObject o)
        {
            var prop = o.Properties().FirstOrDefault(p => string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase));
            return prop?.Value?.ToString();
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private static string GuessInitialSaveDirectory(Type defType)
        {
            // Try to place under the first mod and the appropriate content folder if annotated
            string modsRoot = Path.Combine(Application.dataPath, "..", "Mods");
            string folder = defType.GetCustomAttribute<ContentFolderAttribute>()?.FolderName ?? "Content";
            if (Directory.Exists(modsRoot))
            {
                var candidate = Directory.GetDirectories(modsRoot).FirstOrDefault();
                if (!string.IsNullOrEmpty(candidate))
                {
                    var path = Path.Combine(candidate, folder);
                    return path;
                }
            }
            return modsRoot;
        }

        // ---------- Duplicate ----------
        private void DuplicateSelection()
        {
            if (!(_selectedObj is IContentDef def) || def == null) return;

            var srcObj = _selectedObj as UnityEngine.Object;
            var newObj = ScriptableObject.CreateInstance(srcObj.GetType());
            // Copy serializable fields
            EditorUtility.CopySerialized(srcObj, newObj);

            if (newObj is IContentDef newDef)
            {
                var so = newObj as ScriptableObject;
                // Name hint for user
                if (so != null)
                {
                    so.name = (so.name ?? def.Id ?? "Content") + " Copy";
                }

                // Ensure unique Id programmatically (not exposed as editable field)
                if (newObj is ContentDef newDefConcrete)
                {
                    var baseId = string.IsNullOrWhiteSpace(def.Id) ? (so?.name ?? "NewDef") : def.Id;
                    newDefConcrete.Id = SuggestNewId(baseId);
                }
                // Default to same file path for convenience (arrays will append on save)
                newDef.SourceFile = def.SourceFile;

                // Register and show
                ContentCatalogue.Instance.AddOrReplace(newDef);
                RefreshCurrentDefs();
                SelectObject(newObj);
                _dirtySelection = true; // prompt to save
            }
        }

        private string SuggestNewId(string baseId)
        {
            if (string.IsNullOrWhiteSpace(baseId)) baseId = "NewDef";
            string candidate = baseId + "_Copy";
            int n = 2;
            while (IdExists(candidate))
            {
                candidate = baseId + "_Copy" + n;
                n++;
            }
            return candidate;
        }

        private bool IdExists(string id)
        {
            foreach (var t in _leafDefTypes)
            {
                var m = typeof(ContentCatalogue).GetMethod(nameof(ContentCatalogue.GetAll)).MakeGenericMethod(t);
                var seq = (IEnumerable<IContentDef>)m.Invoke(ContentCatalogue.Instance, null);
                if (seq.Any(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }

        // ---------- Editor-time reload ----------
        private void ReloadModsInEditor()
        {
            try
            {
                ContentCatalogue.Instance.Clear();

                string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
                string modsRoot = Path.Combine(projectRoot, "Mods");

                var source = new FolderModSource(modsRoot);

                var go = new GameObject("EditorJsonImporter_Temp")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                try
                {
                    var importer = go.AddComponent<JsonContentImporter>();
                    foreach (var handle in source.Discover())
                    {
                        importer.Import(handle);
                    }
                }
                finally
                {
                    GameObject.DestroyImmediate(go);
                }

                RefreshCurrentDefs();
                ShowNotification(new GUIContent("Mods reloaded"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Editor reload failed: {ex.Message}\n{ex}");
                EditorUtility.DisplayDialog("Reload Mods Failed", ex.Message, "OK");
            }
        }
    }
}
namespace JG.GameContent.EditorTools
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    internal sealed class UnityIgnoreResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = base.CreateProperties(type, memberSerialization);
            // Filter out any UnityEngine.Object-typed members and a couple of editor-only metadata names
            return props.Where(p => !typeof(UnityEngine.Object).IsAssignableFrom(p.PropertyType)
                                    && !string.Equals(p.PropertyName, "name", StringComparison.Ordinal)
                                    && !string.Equals(p.PropertyName, "SourceFile", StringComparison.Ordinal))
                        .ToList();
        }
    }
}
