using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AssetsFromDirectoryAttribute : Attribute
{
    /// <summary>Name of the member providing the relative directory path.</summary>
    public string DirectoryKey { get; }

    /// <summary>Name of the member providing file extensions (semicolon/comma separated).</summary>
    public string ExtensionKey { get; }

    /// <summary>Name of the member providing a boolean that toggles recursive search.</summary>
    public string IncludeSubdirectoriesKey { get; }

    /// <summary>If true, missing directories/files are treated as optional.</summary>
    public bool Optional { get; }

    public AssetsFromDirectoryAttribute(string directoryKey, string extensionKey, bool optional = false)
        : this(directoryKey, extensionKey, includeSubdirectoriesKey: null, optional: optional)
    {
    }

    public AssetsFromDirectoryAttribute(string directoryKey,
                                        string extensionKey,
                                        string includeSubdirectoriesKey,
                                        bool optional = false)
    {
        if (string.IsNullOrWhiteSpace(directoryKey))
            throw new ArgumentNullException(nameof(directoryKey));
        if (string.IsNullOrWhiteSpace(extensionKey))
            throw new ArgumentNullException(nameof(extensionKey));

        DirectoryKey = directoryKey;
        ExtensionKey = extensionKey;
        IncludeSubdirectoriesKey = includeSubdirectoriesKey;
        Optional = optional;
    }
}
