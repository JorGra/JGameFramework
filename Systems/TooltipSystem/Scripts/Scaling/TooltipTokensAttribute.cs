using System;

namespace JG.GameContent.Tooltips
{
    /// <summary>
    /// Marks a string field/property whose body supports inline tooltip tokens
    /// like <c>{cur:id}</c>, <c>{base:id}</c>, and <c>{delta:id}</c>.
    /// Surfaced in the JSON schema as <c>x-supports-tooltip-tokens: true</c>
    /// so ModCreator can show an "Available tokens" hint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class TooltipTokensAttribute : Attribute { }
}
