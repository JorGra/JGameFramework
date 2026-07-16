using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Vfx
{
    /// <summary>
    /// Closed set of base particle materials, referenced from JSON by id.
    /// Shaders must be known at build time; defs pick a base material and
    /// customize texture/tint at runtime. Place the asset at
    /// Resources/JGameFramework/VfxMaterialLibrary so it ships in every build.
    /// </summary>
    [CreateAssetMenu(menuName = "JGameFramework/Vfx/Material Library", fileName = "VfxMaterialLibrary")]
    public class VfxMaterialLibrary : ScriptableObject
    {
        public const string ResourcesPath = "JGameFramework/VfxMaterialLibrary";

        [Serializable]
        public class Entry
        {
            public string id;
            public Material material;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<string, Material> _lookup;
        private static VfxMaterialLibrary _instance;
        private static bool _searched;

        public static VfxMaterialLibrary Instance
        {
            get
            {
                if (_instance == null && !_searched)
                {
                    _searched = true;
                    _instance = Resources.Load<VfxMaterialLibrary>(ResourcesPath);
                    if (_instance == null)
                    {
                        // Placement-tolerant fallback: accept the asset in any Resources folder.
                        var all = Resources.LoadAll<VfxMaterialLibrary>(string.Empty);
                        if (all.Length > 0)
                            _instance = all[0];
                    }
                    if (_instance == null)
                        Debug.LogWarning($"[Vfx] No VfxMaterialLibrary found in any Resources folder " +
                                         $"(expected Resources/{ResourcesPath}). Particle defs cannot resolve base materials.");
                }
                return _instance;
            }
        }

        public bool TryResolve(string id, out Material material)
        {
            material = null;
            if (string.IsNullOrWhiteSpace(id))
                return false;

            _lookup ??= BuildLookup();
            return _lookup.TryGetValue(id, out material) && material != null;
        }

        private Dictionary<string, Material> BuildLookup()
        {
            var lookup = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    continue;
                lookup[entry.id] = entry.material;
            }
            return lookup;
        }
    }
}
