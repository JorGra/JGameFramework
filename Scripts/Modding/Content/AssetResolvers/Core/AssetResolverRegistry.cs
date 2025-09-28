using System;
using System.Collections.Generic;
using System.Linq;
using JGameFramework.Scripts.Modding.Content.AssetResolvers.Resolvers;

namespace JG.GameContent.AssetResolving
{
    /// Registry of per-extension resolvers. New resolvers register here.
    public static class AssetResolverRegistry
    {
        private static readonly List<IPathAssetResolver> _plugins = new();

        static AssetResolverRegistry()
        {
            // Built-ins
            Register(new ImageAssetResolver());
            Register(new TextAssetResolver());
        }

        public static void Register(IPathAssetResolver plugin)
        {
            if (plugin == null) return;
            _plugins.Add(plugin);
        }

        public static IReadOnlyList<IPathAssetResolver> GetAllResolvers()
        {
            return _plugins.ToArray();
        }

        public static IReadOnlyList<AssetResolverDescriptor> GetResolverDescriptors()
        {
            return _plugins
                .OfType<IDescribedPathAssetResolver>()
                .Select(r => {
                    try
                    {
                        return r.Describe();
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(d => d != null)
                .ToArray();
        }
        public static bool TryGetByExtension(string ext, out IPathAssetResolver resolver)
        {
            var e = NormalizeExt(ext);
            resolver = _plugins.FirstOrDefault(p => p.SupportsExtension(e));
            return resolver != null;
        }

        private static string NormalizeExt(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext)) return string.Empty;
            ext = ext.Trim();
            if (!ext.StartsWith(".")) ext = "." + ext;
            return ext.ToLowerInvariant();
        }
    }
}







