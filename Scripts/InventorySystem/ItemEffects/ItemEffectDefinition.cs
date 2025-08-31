using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace JG.Inventory
{
    /// <summary>
    /// POCO used inside <see cref="IInventoryItem"/> to describe an effect and its JSON-serialised parameters.
    /// </summary>
    [Serializable]
    public class ItemEffectDefinition
    {
        [Tooltip("Concrete class name, e.g. HealEffect")]
        public string effectType;

        [Tooltip("Parameters object understood by the effect (JSON object).")]
        public JToken effectParams;
    }
}
