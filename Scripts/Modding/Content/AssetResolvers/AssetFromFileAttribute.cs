using System;

/// Tells the asset resolver to load an asset for this member.
/// Two modes are supported:
/// 1) File mode (default):
///        <ModRoot>/<SubFolder>/<FileName><Extension>
///    - Good for loose mod files (PNG, WAV, etc.)
/// 2) Resources mode (prefix SubFolder with "Resources:"):
///        Resources/<SubPath>/<FileName>   (no extension)
///    - Good for Unity-built assets like Prefabs, Materials, Sprites.
///      Example: SubFolder = "Resources:Prefabs/Towers", FileName="BaseTurret"
///               => Resources.Load("Prefabs/Towers/BaseTurret", <MemberType>)
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AssetFromFileAttribute : Attribute
{
    /// Folder path inside the mod (file mode), e.g. "Items/Icons" or "Sfx",
    /// OR prefixed with "Resources:" to enable Resources mode, e.g. "Resources:Prefabs/Towers".
    public string SubFolder { get; }

    /// File extension (with dot, e.g. ".png") for file mode.
    /// If null the resolver picks a sensible default based on member type.
    /// Ignored in Resources mode.
    public string Extension { get; }

    /// Optional: name of a string field/property on the same definition whose value
    /// provides the file name / logical path segment. If null, the definition’s Id is used.
    public string FileNameKey { get; }

    /// If true, missing assets only raise a warning instead of error.
    public bool Optional { get; }

    public AssetFromFileAttribute(string subFolder,
                                  string extension = null,
                                  string fileNameKey = null,
                                  bool optional = false)
    {
        SubFolder = subFolder;
        Extension = extension;
        FileNameKey = fileNameKey;
        Optional = optional;
    }
}
