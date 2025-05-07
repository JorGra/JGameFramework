using System.Collections.Generic;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Immutable static description of an item.  
    /// Icons are loaded lazily from <c>Resources/Icons/Icons/{itemId}.png</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Gameplay/Inventory/Item")]
    public class ItemData : ScriptableObject
    {
        /* ────── static cache ────── */

        private static readonly Dictionary<string, Sprite> iconCache = new();

        /* ────── serialized fields ────── */

        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("(Optional) Editor-time icon. If left empty it will be loaded at runtime.")]
        [SerializeField] private Sprite icon;
        [SerializeField] private int maxStack = 1;
        [SerializeField] private List<string> equipTags = new();
        [SerializeField] private List<ItemEffectDefinition> effects = new();

        /* ────── public API ────── */

        public string Id => id;
        public string DisplayName => displayName;

        /// <summary>
        /// Returns the icon, loading it from <c>Resources/Icons/Icons</c> if necessary.
        /// </summary>
        public Sprite Icon
        {
            get
            {
                if (icon != null) return icon;                 // designer-assigned

                /* lazy-load & cache */
                if (iconCache.TryGetValue(id, out var cached)) return cached;

                string path = $"Items/Icons/{id}";
                cached = Resources.Load<Sprite>(path);

                if (cached == null)
                    Debug.LogWarning($"[ItemData] Icon not found at Resources/{path}.png");

                iconCache[id] = cached;
                return cached;
            }
        }

        public int MaxStack => Mathf.Max(1, maxStack);
        public IReadOnlyList<string> EquipTags => equipTags;
        public IReadOnlyList<ItemEffectDefinition> Effects => effects;
    }
}
