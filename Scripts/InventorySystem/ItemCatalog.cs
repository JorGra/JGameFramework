using System.Collections.Generic;
using System.IO;
using JG.Inventory;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Singleton service exposing a lookup table of every <see cref="ItemData"/>.
    /// </summary>
    public class ItemCatalog : MonoBehaviour
    {
        public static ItemCatalog Instance { get; private set; }

        readonly Dictionary<string, ItemData> items = new();

        public Dictionary<string, ItemData> Items => items;

        void Awake()
        {
            // Singleton boilerplate
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadResourcesItems();
            LoadJsonItems();
        }

        /// <summary>Fetch by ID; returns null if not found.</summary>
        public ItemData Get(string id) => items.TryGetValue(id, out var data) ? data : null;

        void LoadResourcesItems()
        {
            foreach (var item in Resources.LoadAll<ItemData>("Items"))
            {
                if (!items.ContainsKey(item.Id))
                    items[item.Id] = item;
            }
        }

        void LoadJsonItems()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Items");
            if (!Directory.Exists(dir)) return;

            foreach (string path in Directory.EnumerateFiles(dir, "*.json"))
            {
                string json = File.ReadAllText(path);
                JsonItemRoot root = JsonUtility.FromJson<JsonItemRoot>(json);
                ItemData so = ScriptableObject.CreateInstance<ItemData>();

                // Use reflection/binding flags to set private serialized fields
                ReflectionUtil.SetPrivateField(so, "id", root.id);
                ReflectionUtil.SetPrivateField(so, "displayName", root.displayName);
                ReflectionUtil.SetPrivateField(so, "maxStack", root.maxStack);
                ReflectionUtil.SetPrivateField(so, "equipTags", root.equipTags);
                ReflectionUtil.SetPrivateField(so, "effects", root.effects);

                if (!items.ContainsKey(so.Id))
                    items[so.Id] = so;
            }
        }

        [ContextMenu("Print All Items")]
        void PrintItems()
        {
            foreach (var item in items)
            {
                Debug.Log($"Item ID: {item.Key}, Name: {item.Value.DisplayName}");
            }
        }

        #region JSON DTO
        [System.Serializable]
        private class JsonItemRoot
        {
            public string id;
            public string displayName;
            public int maxStack = 1;
            public List<string> equipTags = new();
            public List<ItemEffectDefinition> effects = new();
        }
        #endregion
    }
}
