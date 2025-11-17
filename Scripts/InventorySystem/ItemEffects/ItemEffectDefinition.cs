// Kept for backward compatibility with existing JSON files. Prefer ItemEffectDef.
using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace JG.Inventory
{
    [Serializable]
    public class ItemEffectDefinition
    {
        [Tooltip("Concrete class name, e.g. HealEffect")]
        public string effectType;

        [Tooltip("Parameters object understood by the effect (JSON object).")]
        public JToken effectParams;
    }
}
