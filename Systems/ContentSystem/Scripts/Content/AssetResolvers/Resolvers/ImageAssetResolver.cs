using System;
using System.IO;
using UnityEngine;
using JG.GameContent.AssetResolving;

namespace JGameFramework.Scripts.Modding.Content.AssetResolvers.Resolvers
{
    /// Handles image file formats such as PNG and JPG.
    internal sealed class ImageAssetResolver : IDescribedPathAssetResolver
    {
        private static readonly string[] exts = new[] { ".png", ".jpg", ".jpeg" };

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
            using var _ = JG.GameContent.Diagnostics.LoadProfiler.Measure(JG.GameContent.Diagnostics.LoadProfiler.ImageDecode);

            var data = File.ReadAllBytes(absolutePath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(data, markNonReadable: false))
                throw new Exception($"Failed to decode image: {absolutePath}");
            tex.name = Path.GetFileNameWithoutExtension(absolutePath);

            // If target wants a Sprite, create one on the fly.
            if (targetType != null && typeof(Sprite).IsAssignableFrom(targetType))
            {
                return Sprite.Create(tex,
                                     new Rect(0, 0, tex.width, tex.height),
                                     new Vector2(0.5f, 0.5f),
                                     100f);
            }

            return tex;
        }

        public UnityEngine.Object LoadFromResources(string resourcesPathNoExt, Type targetType)
        {
            // Prefer Sprite if requested, otherwise Texture2D.
            if (targetType != null && typeof(Sprite).IsAssignableFrom(targetType))
            {
                return Resources.Load<Sprite>(resourcesPathNoExt);
            }

            var tex = Resources.Load<Texture2D>(resourcesPathNoExt);
            if (tex != null) return tex;

            // Fallback: try Sprite (import settings may produce Sprites).
            var sprite = Resources.Load<Sprite>(resourcesPathNoExt);
            return sprite;
        }

        public AssetResolverDescriptor Describe()
        {
            return new AssetResolverDescriptor(
                id: "image",
                displayName: "Image File",
                extensions: exts,
                previewKind: "image",
                supportedTypes: new[] { typeof(Texture2D), typeof(Sprite) }
            );
        }
    }
}






