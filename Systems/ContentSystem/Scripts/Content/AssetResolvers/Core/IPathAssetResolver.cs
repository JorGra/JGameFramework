using System;
using UnityEngine;

namespace JG.GameContent.AssetResolving
{
    /// Plugin that knows how to load assets for specific file extensions.
    /// Implementations should be small and focused (e.g., images, text).
    public interface IPathAssetResolver
    {
        /// Returns true if this resolver supports the given extension (with dot, lowercase).
        bool SupportsExtension(string ext);

        /// Loads an asset from a physical file path (absolute path on disk).
        /// Target type is the member type the asset will be assigned to; use as a hint.
        UnityEngine.Object LoadFromFile(string absolutePath, Type targetType);

        /// Loads an asset from the Unity Resources database using a path WITHOUT extension.
        /// Implementations may use target type to choose which resource type to load.
        UnityEngine.Object LoadFromResources(string resourcesPathNoExt, Type targetType);
    }
}

