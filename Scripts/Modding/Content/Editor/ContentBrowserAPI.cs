using System;
using System.Linq;
using System.Reflection;
using JG.GameContent;
using UnityEditor;
using UnityEngine;

namespace JG.GameContent.EditorTools
{
    /// Helper to control ContentBrowserWindow without modifying its internals.
    internal static class ContentBrowserAPI
    {
        public static bool FocusAndSelectById(Type targetType, string id)
        {
            if (targetType == null || string.IsNullOrWhiteSpace(id)) return false;
            var win = EditorWindow.GetWindow(typeof(ContentBrowserWindow)) as EditorWindow;
            if (win == null) return false;
            win.titleContent = new GUIContent("Content Browser");
            win.Show();
            win.Focus();

            // Resolve object first
            UnityEngine.Object obj = null;
            try
            {
                var m = typeof(ContentCatalogue).GetMethod(nameof(ContentCatalogue.GetAll)).MakeGenericMethod(targetType);
                var seq = (System.Collections.Generic.IEnumerable<IContentDef>)m.Invoke(ContentCatalogue.Instance, null);
                var def = seq.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));
                obj = def as UnityEngine.Object;
            }
            catch { }

            // Switch filter
            try
            {
                var getTreeId = typeof(ContentBrowserWindow).GetMethod("GetTreeIdForType", BindingFlags.Instance | BindingFlags.NonPublic);
                int typeTreeId = (int)getTreeId.Invoke(win, new object[] { obj ? obj.GetType() : targetType });

                var fSel = typeof(ContentBrowserWindow).GetField("_selectedTypeTreeId", BindingFlags.Instance | BindingFlags.NonPublic);
                fSel?.SetValue(win, typeTreeId);
                var refresh = typeof(ContentBrowserWindow).GetMethod("RefreshCurrentDefs", BindingFlags.Instance | BindingFlags.NonPublic);
                refresh?.Invoke(win, null);

                if (obj)
                {
                    var selectObj = typeof(ContentBrowserWindow).GetMethod("SelectObject", BindingFlags.Instance | BindingFlags.NonPublic);
                    selectObj?.Invoke(win, new object[] { obj });
                    return true;
                }
                else
                {
                    var fSearch = typeof(ContentBrowserWindow).GetField("_search", BindingFlags.Instance | BindingFlags.NonPublic);
                    fSearch?.SetValue(win, id);
                    win.Repaint();
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void FocusAndSearch(string query, Type preferredType = null)
        {
            var win = EditorWindow.GetWindow(typeof(ContentBrowserWindow)) as EditorWindow;
            if (win == null) return;
            win.titleContent = new GUIContent("Content Browser");
            win.Show();
            win.Focus();
            try
            {
                if (preferredType != null)
                {
                    var getTreeId = typeof(ContentBrowserWindow).GetMethod("GetTreeIdForType", BindingFlags.Instance | BindingFlags.NonPublic);
                    int typeTreeId = (int)getTreeId.Invoke(win, new object[] { preferredType });
                    var fSel = typeof(ContentBrowserWindow).GetField("_selectedTypeTreeId", BindingFlags.Instance | BindingFlags.NonPublic);
                    fSel?.SetValue(win, typeTreeId);
                }
                else
                {
                    var fSel = typeof(ContentBrowserWindow).GetField("_selectedTypeTreeId", BindingFlags.Instance | BindingFlags.NonPublic);
                    fSel?.SetValue(win, 0);
                }
                var refresh = typeof(ContentBrowserWindow).GetMethod("RefreshCurrentDefs", BindingFlags.Instance | BindingFlags.NonPublic);
                refresh?.Invoke(win, null);
                var fSearch = typeof(ContentBrowserWindow).GetField("_search", BindingFlags.Instance | BindingFlags.NonPublic);
                fSearch?.SetValue(win, query ?? string.Empty);
                win.Repaint();
            }
            catch { }
        }
    }
}

