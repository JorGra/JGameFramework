using System;
using System.Collections.Generic;

namespace JG.GameContent
{
    public static class DiscriminatorConverterRegistry
    {
        private static readonly HashSet<Action> _resetters = new();

        public static void Register(Action resetter)
        {
            _resetters.Add(resetter);
        }

        public static void ResetAll()
        {
            foreach (var r in _resetters)
                r();
        }
    }
}
