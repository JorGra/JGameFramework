using System;
using System.IO;
using UnityEngine;
using JG.GameContent.AssetResolving;

namespace JGameFramework.Scripts.Modding.Content.AssetResolvers.Resolvers
{
    /// Resolves common text-like assets (txt, json, csv) to TextAsset.
    internal sealed class TextAssetResolver : IPathAssetResolver
    {
        private static readonly string[] exts = new[] { ".txt", ".json", ".csv", ".bytes" };

        public bool SupportsExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return false;
            ext = ext.ToLowerInvariant();
            foreach (var e in exts)
                if (ext == e) return true;
            return false;
        }

        public UnityEngine.Object LoadFromFile(string absolutePath, Type targetType)
        {
            // Read as text if possible, otherwise as bytes.
            if (string.Equals(Path.GetExtension(absolutePath), ".bytes", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = File.ReadAllBytes(absolutePath);
                var ta = new TextAsset(System.Text.Encoding.UTF8.GetString(bytes));
                ta.name = Path.GetFileNameWithoutExtension(absolutePath);
                return ta;
            }
            else
            {
                var text = File.ReadAllText(absolutePath);
                var ta = new TextAsset(text) { name = Path.GetFileNameWithoutExtension(absolutePath) };
                return ta;
            }
        }

        public UnityEngine.Object LoadFromResources(string resourcesPathNoExt, Type targetType)
        {
            return Resources.Load<TextAsset>(resourcesPathNoExt);
        }
    }
}

