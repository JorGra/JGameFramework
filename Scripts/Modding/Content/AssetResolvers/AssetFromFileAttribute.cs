using System;

/// Tell the asset resolver to load a file:
///     <ModRoot>/<SubFolder>/<FileName><Extension>
/// `FileName` is either the definition’s `Id` (default) **or** the value of
/// another JSON‑populated field/property you specify via `FileNameKey`.
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AssetFromFileAttribute : Attribute
{
    /// Folder path inside the mod, e.g. "Items/Icons" or "Skills/SFX"
    public string SubFolder { get; }

    /// File extension to append (include the dot, e.g. ".png").
    /// If null the resolver picks a sensible default per asset type.
    public string Extension { get; }

    /// Name of a *string* member on the same class whose value is used instead
    /// of `Id` to build the file name.  Allows several sprites per definition.
    public string FileNameKey { get; }

    /// If true, missing files only raise a warning.
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
