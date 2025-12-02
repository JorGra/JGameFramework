using System;
using UnityEngine;

namespace UI.Theming
{
    public enum ThemeKeyKind
    {
        Color,
        Sprite,
        Font,
        Style
    }

    /// <summary>
    /// Annotate string fields that should expose a themed key picker in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ThemeKeyAttribute : PropertyAttribute
    {
        public ThemeKeyKind Kind { get; }
        public Type StyleType { get; }

        public ThemeKeyAttribute(ThemeKeyKind kind)
        {
            Kind = kind;
        }

        public ThemeKeyAttribute(Type styleType)
        {
            Kind = ThemeKeyKind.Style;
            StyleType = styleType;
        }
    }
}
