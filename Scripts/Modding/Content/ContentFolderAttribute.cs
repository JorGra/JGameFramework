using System;

namespace JG.GameContent
{
    /// <summary>
    /// Annotate a <c>ContentDef</c> subtype with the sub‑folder mods should place
    /// JSON files in. E.g. "Items", "Enemies".
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ContentFolderAttribute : Attribute
    {
        public string FolderName { get; }

        public ContentFolderAttribute(string folderName) => FolderName = folderName;
    }
}
