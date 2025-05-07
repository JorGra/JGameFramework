using System;

namespace JG.Inventory
{
    /// <summary>
    /// Put this on an IItemEffect implementation to declare the
    /// external identifier used inside item JSON.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ItemEffectAttribute : Attribute
    {
        public string Id { get; }
        public ItemEffectAttribute(string id) => Id = id;
    }
}
