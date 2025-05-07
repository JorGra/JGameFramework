using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JG.Inventory
{
    /// <summary>
    /// Read-only database of every ItemData known at runtime.
    ///
    /// Load order  (later sources override former ones on duplicate ids):
    ///   1) ItemData ScriptableObjects in  Resources/Items/
    ///   2) TextAsset JSON files listed in the Inspector (default:  Resources/Items/items.json)
    ///   3) *.json files found in the StreamingAssets sub-folders listed in the Inspector
    ///
    /// Each JSON file may contain either
    ///   • **one** item object       – legacy format (root = JsonItemRoot)
    ///   • **many** item objects     – new format (root = JsonItemFile { items:[ … ] })
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ItemCatalog : MonoBehaviour
    {
        /* ---------------- inspector ---------------- */

        [Tooltip("TextAssets under Resources to import, WITHOUT file extension.\n" +
                 "Example:  \"Items/items\"  loads  Resources/Items/items.json")]
        [SerializeField] private List<string> resourceJsonFiles = new() { "Items/items" };

        [Tooltip("StreamingAssets sub-folders whose *.json files are imported in order.\n" +
                 "Add mod folders to this list (or call ImportModFile at runtime).")]
        [SerializeField] private List<string> streamingJsonDirs = new() { "Items" };

        /* ---------------- data store ---------------- */

        public static ItemCatalog Instance { get; private set; }

        private readonly Dictionary<string, ItemData> items = new();
        public IReadOnlyDictionary<string, ItemData> Items => items;

        /* ---------------- life-cycle ---------------- */

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            /* 1 ─ designer-authored ScriptableObjects */
            LoadSOsFromResources();

            /* 2 ─ explicit JSON TextAssets */
            foreach (var resPath in resourceJsonFiles)
                LoadJsonFromResources(resPath);

            /* 3 ─ JSON inside StreamingAssets */
            foreach (var dir in streamingJsonDirs)
                LoadJsonDir(Path.Combine(Application.streamingAssetsPath, dir));
        }

        /* ---------------- public API ---------------- */

        /// <summary>Lookup by id; returns <c>null</c> if not found.</summary>
        public ItemData Get(string id) =>
            items.TryGetValue(id, out var d) ? d : null;

        /// <summary>
        /// Import an external JSON file at runtime (e.g. downloaded mod).
        /// Later imports override earlier entries if <paramref name="canOverride"/> is true.
        /// </summary>
        public bool ImportModFile(string fullPath, bool canOverride = true)
        {
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[ItemCatalog] File not found: {fullPath}");
                return false;
            }
            string json = File.ReadAllText(fullPath);
            return LoadJson(json, canOverride);
        }

        /* ---------------- internal loaders ---------------- */

        void LoadSOsFromResources()
        {
            foreach (var so in Resources.LoadAll<ItemData>("Items"))
                AddOrSkip(so, canOverride: true);
        }

        void LoadJsonFromResources(string pathWithoutExt)
        {
            TextAsset ta = Resources.Load<TextAsset>(pathWithoutExt);
            if (ta == null)
            {
                Debug.LogWarning($"[ItemCatalog] Resources file not found: {pathWithoutExt}.json");
                return;
            }
            LoadJson(ta.text, canOverride: true);
        }

        void LoadJsonDir(string absDir)
        {
            if (!Directory.Exists(absDir)) return;

            foreach (string file in Directory.EnumerateFiles(absDir, "*.json"))
                LoadJson(File.ReadAllText(file), canOverride: true);
        }

        /* ---------------- JSON dispatcher ---------------- */

        /// <summary>
        /// Accepts either a single-item JSON or a wrapper with many items.
        /// </summary>
        bool LoadJson(string json, bool canOverride)
        {
            /* try multi-item file first */
            var multi = JsonUtility.FromJson<JsonItemFile>(json);
            if (multi != null && multi.items != null && multi.items.Count > 0)
            {
                foreach (var def in multi.items)
                    CreateAndAdd(def, canOverride);
                return true;
            }

            /* fallback – legacy one-item file */
            var single = JsonUtility.FromJson<JsonItemRoot>(json);
            if (single == null || string.IsNullOrWhiteSpace(single.id))
            {
                Debug.LogError("[ItemCatalog] JSON contained no valid item definitions.");
                return false;
            }
            CreateAndAdd(single, canOverride);
            return true;
        }

        /* ---------------- helpers ---------------- */

        void CreateAndAdd(JsonItemRoot src, bool canOverride)
        {
            var so = ScriptableObject.CreateInstance<ItemData>();
            ReflectionUtil.SetPrivateField(so, "id", src.id);
            ReflectionUtil.SetPrivateField(so, "displayName", src.displayName);
            ReflectionUtil.SetPrivateField(so, "maxStack", src.maxStack);
            ReflectionUtil.SetPrivateField(so, "equipTags", src.equipTags);
            ReflectionUtil.SetPrivateField(so, "effects", src.effects);

            AddOrSkip(so, canOverride);
        }

        bool AddOrSkip(ItemData so, bool canOverride)
        {
            if (items.ContainsKey(so.Id))
            {
                if (!canOverride) return false;          // keep older entry
                items[so.Id] = so;                       // override
            }
            else items.Add(so.Id, so);
            return true;
        }

        /* ---------------- DTOs ---------------- */

        /// <summary>Single item object (legacy &amp; array element).</summary>
        [System.Serializable]
        private class JsonItemRoot
        {
            public string id;
            public string displayName;
            public int maxStack = 1;
            public List<string> equipTags = new();
            public List<ItemEffectDefinition> effects = new();
        }

        /// <summary>
        /// Wrapper that allows multiple item objects plus future metadata.
        /// {
        ///   "items":[ {…}, {…} ],
        ///   "modName":"AwesomeMod",
        ///   "formatVersion":1
        /// }
        /// </summary>
        [System.Serializable]
        private class JsonItemFile
        {
            public List<JsonItemRoot> items = new();
            /* optional user metadata fields go here */
        }
    }
}
