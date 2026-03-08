using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.GameContent.Debugging
{
    public readonly struct DebugCommand
    {
        public readonly string Name;
        public readonly string Category;
        public readonly Action Callback;

        public DebugCommand(string name, Action callback, string category = null)
        {
            Name = name;
            Category = category ?? "General";
            Callback = callback;
        }
    }

    public static class DebugCommandRegistry
    {
        public static event Action OnCommandsChanged;

        private static readonly List<DebugCommand> _commands = new();
        private static bool _defaultsRegistered;

        public static IReadOnlyList<DebugCommand> Commands => _commands;

        public static void Register(string name, Action callback, string category = null)
        {
            // Avoid duplicates
            for (int i = 0; i < _commands.Count; i++)
            {
                if (_commands[i].Name == name)
                {
                    _commands[i] = new DebugCommand(name, callback, category);
                    OnCommandsChanged?.Invoke();
                    return;
                }
            }

            _commands.Add(new DebugCommand(name, callback, category));
            OnCommandsChanged?.Invoke();
        }

        public static void Unregister(string name)
        {
            for (int i = _commands.Count - 1; i >= 0; i--)
            {
                if (_commands[i].Name == name)
                {
                    _commands.RemoveAt(i);
                    OnCommandsChanged?.Invoke();
                    return;
                }
            }
        }

        internal static void InitializeDefaults()
        {
            if (_defaultsRegistered) return;
            _defaultsRegistered = true;

            Register("Log All Content Defs", () =>
            {
                foreach (var def in ContentCatalogue.Instance.GetAllDefs())
                    Debug.Log($"[ContentDef] {def.GetType().Name}: {def.Id}");
            }, "Content");
        }
    }
}
