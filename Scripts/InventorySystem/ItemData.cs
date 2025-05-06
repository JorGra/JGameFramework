using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Immutable static description of an item (loaded from JSON or pre-authored).
    /// </summary>
    [CreateAssetMenu(menuName = "Gameplay/Inventory/Item")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string id;            // unique key, e.g. "potion_health_small"
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private int maxStack = 1;
        [SerializeField] private List<string> equipTags = new();     // e.g. ["Head"], ["Weapon","TwoHanded"]
        [SerializeField] private List<ItemEffectDefinition> effects = new();

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaxStack => Mathf.Max(1, maxStack);
        public IReadOnlyList<string> EquipTags => equipTags;
        public IReadOnlyList<ItemEffectDefinition> Effects => effects;
    }
}
