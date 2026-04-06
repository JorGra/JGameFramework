#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JG.GameContent.SchemaExport
{
    [CustomEditor(typeof(ContentSchemaExporterSettings))]
    internal sealed class ContentSchemaExporterSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty outputPathIsRelative;
        private SerializedProperty outputPath;

        private void OnEnable()
        {
            outputPathIsRelative = serializedObject.FindProperty("outputPathIsRelative");
            outputPath = serializedObject.FindProperty("outputPath");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawOutputPathField();
            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject, "m_Script", "outputPath", "outputPathIsRelative",
                "useProjectVersion", "gameVersion");
            DrawVersionField();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVersionField()
        {
            var useProjectVersion = serializedObject.FindProperty("useProjectVersion");
            EditorGUILayout.PropertyField(useProjectVersion, new GUIContent("Use Project Version"));

            if (useProjectVersion.boolValue)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("Game Version", Application.version);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("gameVersion"),
                    new GUIContent("Game Version"));
            }
        }

        private void DrawOutputPathField()
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(outputPath, new GUIContent("Schema Output Folder"));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                var current = outputPath.stringValue;
                if (!string.IsNullOrWhiteSpace(current) && !Path.IsPathRooted(current))
                    current = Path.Combine(projectRoot, current);

                var selected = EditorUtility.OpenFolderPanel("Select Schema Output Folder",
                    Directory.Exists(current) ? current : projectRoot, "");

                if (!string.IsNullOrEmpty(selected))
                {
                    var normalized = selected.Replace('\\', '/');
                    var root = projectRoot.Replace('\\', '/');

                    if (normalized.StartsWith(root + "/"))
                    {
                        outputPath.stringValue = normalized.Substring(root.Length + 1);
                        outputPathIsRelative.boolValue = true;
                    }
                    else
                    {
                        outputPath.stringValue = normalized;
                        outputPathIsRelative.boolValue = false;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            var label = outputPathIsRelative.boolValue ? "Relative to project root" : "Absolute path";
            EditorGUILayout.LabelField(" ", label, EditorStyles.miniLabel);
        }
    }
}
#endif
