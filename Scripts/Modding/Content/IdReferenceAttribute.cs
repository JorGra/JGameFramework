using System;
using UnityEngine;

namespace JG.GameContent
{
    /// Marks a string field/property as a reference to a content definition Id.
    /// The referenced type must inherit from ContentDef.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IdReferenceAttribute : PropertyAttribute
    {
        public Type TargetType { get; }
        public bool Optional { get; }

        public IdReferenceAttribute(Type targetType, bool optional = false)
        {
            TargetType = targetType;
            Optional = optional;
        }
    }
}
