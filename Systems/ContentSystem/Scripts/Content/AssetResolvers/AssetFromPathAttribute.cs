using System;

/// Marks a field/property to be populated from a path key contained
/// in the same definition object. The key should be a mod-relative path
/// (relative to the current content folder), e.g. "/icons/health.png",
/// or start with "Resources:" to load from the Resources database,
/// e.g. "Resources:UI/Icons/health" or "Resources:UI/Icons/health.png".
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AssetFromPathAttribute : Attribute
{
    /// Name of a string field/property on the same definition holding the key/path.
    public string PathKey { get; }

    /// If true, missing assets only raise a warning instead of error.
    public bool Optional { get; }

    public AssetFromPathAttribute(string pathKey, bool optional = false)
    {
        PathKey = pathKey;
        Optional = optional;
    }
}

