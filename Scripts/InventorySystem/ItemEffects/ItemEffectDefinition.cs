using System;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// POCO used inside <see cref="ItemData"/> to describe an effect and its JSON-serialised parameters.
    /// </summary>
    [Serializable]
    public class ItemEffectDefinition
    {
        [Tooltip("Concrete class name, e.g. HealEffect")]
        public string effectType;

        [Tooltip("JSON-encoded parameter object understood by the effect.")]
        public string effectParams;
    }
}
