//  ─────────────────────────────────────────────────────────────────────────────
//  ItemCatalogBrowserWindow.cs
//  JG.Inventory  –  simple inspector for all currently loaded items
//  ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JG.Inventory.Editor
{
    public class ItemCatalogBrowserWindow : EditorWindow
    {
        private Vector2 _scroll;
        private readonly Dictionary<string, bool> _foldout = new();   // id → isOpen?

        /* ─────────────────────────  menu  ───────────────────────── */

        [MenuItem("Window/JG Inventory/Item Catalog Browser")]
        public static void Open()
        {
            var win = GetWindow<ItemCatalogBrowserWindow>("Item Catalog");
            win.minSize = new Vector2(350, 300);
        }

        /* ─────────────────────────  GUI  ───────────────────────── */

        private void OnGUI()
        {
            var items = CollectItems();

            if (items == null || items.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No items found.\n" +
                    "• Enter Play Mode to see JSON-loaded items.\n" +
                    "• Or add ItemData ScriptableObjects to Resources/Items.",
                    MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var it in items)
                DrawItem(it);
            EditorGUILayout.EndScrollView();
        }

        /* ─────────────────────────  helpers  ───────────────────────── */

        /// <summary>Gets the currently available items.</summary>
        private List<ItemData> CollectItems()
        {
            if (Application.isPlaying && ItemCatalog.Instance != null)
                return ItemCatalog.Instance.Items.Values
                                           .OrderBy(i => i.DisplayName)
                                           .ToList();

            // edit mode fallback: only pre-authored SOs
            return Resources.LoadAll<ItemData>("Items")
                            .OrderBy(i => i.DisplayName)
                            .ToList();
        }

        private void DrawItem(ItemData it)
        {
            bool open = _foldout.TryGetValue(it.Id, out bool o) && o;
            string header = $"{it.DisplayName}  ({it.Id})";

            var style = EditorStyles.foldout;
            style.fontStyle = FontStyle.Bold;

            open = EditorGUILayout.Foldout(open, header, true, style);
            _foldout[it.Id] = open;

            if (!open) return;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawLabel("Max Stack", it.MaxStack.ToString());
                DrawLabel("Equip Tags",
                          it.EquipTags.Count == 0 ? "–" :
                          string.Join(", ", it.EquipTags));

                // Effects
                if (it.Effects.Count == 0)
                {
                    DrawLabel("Effects", "–");
                }
                else
                {
                    EditorGUILayout.LabelField("Effects");
                    using (new EditorGUI.IndentLevelScope())
                    {
                        foreach (var def in it.Effects)
                        {
                            EditorGUILayout.LabelField(def.effectType);
                            using (new EditorGUI.IndentLevelScope())
                                EditorGUILayout.TextArea(def.effectParams);
                        }
                    }
                }

                GUILayout.Space(4);
            }
            EditorGUILayout.Separator();
        }

        private static void DrawLabel(string name, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, GUILayout.Width(90));
            EditorGUILayout.SelectableLabel(value,
                EditorStyles.wordWrappedLabel,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
