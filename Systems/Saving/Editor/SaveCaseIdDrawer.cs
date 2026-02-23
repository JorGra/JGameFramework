using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JGameFramework.Saving;
using UnityEditor;
using UnityEngine;

namespace JGameFramework.Saving.Editor
{
    [CustomPropertyDrawer(typeof(SaveCaseId))]
    public sealed class SaveCaseIdDrawer : PropertyDrawer
    {
        private static string[] cachedNames;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("value");
            var names = GetKnownCaseNames();

            if (names.Length == 0)
            {
                EditorGUI.PropertyField(position, valueProp, label);
                return;
            }

            int current = Array.IndexOf(names, valueProp.stringValue);
            if (current < 0) current = 0;

            int selected = EditorGUI.Popup(position, label.text, current, names);
            valueProp.stringValue = names[selected];
        }

        private static string[] GetKnownCaseNames()
        {
            if (cachedNames != null) return cachedNames;

            var results = new List<string>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (!type.IsClass || !type.IsAbstract || !type.IsSealed) continue; // static classes

                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                    {
                        if (field.FieldType != typeof(SaveCaseId)) continue;
                        var id = (SaveCaseId)field.GetValue(null);
                        if (!string.IsNullOrEmpty(id.Value))
                            results.Add(id.Value);
                    }
                }
            }

            cachedNames = results.Distinct().OrderBy(n => n).ToArray();
            return cachedNames;
        }
    }
}
