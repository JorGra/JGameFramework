using System;

namespace JG.GameContent
{
    /// <summary>
    /// Marks a string field as translatable. The schema exporter emits
    /// <c>x-translatable: true</c> for annotated members so the ModCreator
    /// and runtime localization bridge know which fields need translations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class TranslatableAttribute : Attribute { }
}
