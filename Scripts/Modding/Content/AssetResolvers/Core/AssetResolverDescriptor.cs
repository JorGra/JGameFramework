using System;
using System.Collections.Generic;
using System.Linq;

namespace JG.GameContent.AssetResolving
{
    /// <summary>
    /// Describes the capabilities of an asset resolver so that tooling can expose richer metadata.
    /// </summary>
    public sealed class AssetResolverDescriptor
    {
        private readonly Type[] _supportedTypes;

        public AssetResolverDescriptor(string id,
                                       string displayName,
                                       IReadOnlyList<string> extensions,
                                       string previewKind,
                                       IReadOnlyList<Type> supportedTypes,
                                       bool supportsResources = true)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Extensions = extensions ?? Array.Empty<string>();
            PreviewKind = previewKind ?? string.Empty;
            _supportedTypes = supportedTypes?.Where(t => t != null).ToArray() ?? Array.Empty<Type>();
            SupportsResources = supportsResources;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> Extensions { get; }
        public string PreviewKind { get; }
        public bool SupportsResources { get; }
        public IReadOnlyList<Type> SupportedTypes => _supportedTypes;

        public bool SupportsType(Type type)
        {
            if (type == null || _supportedTypes.Length == 0)
                return false;

            foreach (var supported in _supportedTypes)
            {
                if (supported == null)
                    continue;

                if (supported == type || supported.IsAssignableFrom(type) || type.IsAssignableFrom(supported))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Optional extension for <see cref="IPathAssetResolver"/> allowing resolvers to describe themselves.
    /// </summary>
    public interface IDescribedPathAssetResolver : IPathAssetResolver
    {
        AssetResolverDescriptor Describe();
    }
}
