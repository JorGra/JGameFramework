using UnityEditor;
using UnityEngine;

namespace JG.GameContent.EditorTools
{
    /// Simple floating inspector to peek/edit another content definition without switching selection.
    public sealed class InlineDefInspectorWindow : EditorWindow
    {
        private UnityEngine.Object _target;
        private UnityEditor.Editor _editor;

        public static void Show(UnityEngine.Object obj)
        {
            if (!obj) return;
            var win = CreateInstance<InlineDefInspectorWindow>();
            win.titleContent = new GUIContent(obj.name);
            win._target = obj;
            win._editor = UnityEditor.Editor.CreateEditor(obj);
            win.ShowUtility();
            win.minSize = new Vector2(320, 200);
        }

        private void OnDisable()
        {
            if (_editor != null)
                DestroyImmediate(_editor);
        }

        private void OnGUI()
        {
            if (!_target || _editor == null)
            {
                EditorGUILayout.HelpBox("Target was destroyed.", MessageType.Info);
                return;
            }
            EditorGUI.BeginChangeCheck();
            _editor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                // No-op; relies on Content Browser save flow.
            }
        }
    }
}

