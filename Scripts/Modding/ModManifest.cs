using System;
using System.Collections.Generic;

namespace JG.Modding
{
    /// <summary>Plain-data container that mirrors a <c>manifest.json</c> file.</summary>
    [Serializable]
    public sealed class ModManifest
    {
        public string id;
        public string name;
        public string version;
        public string author;
        public string description;

        public string[] requires = Array.Empty<string>();
        public string[] loadBefore = Array.Empty<string>();
        public string[] loadAfter = Array.Empty<string>();
    }

    /// <summary>Pair of manifest + handle once discovery succeeds (read-only to callers).</summary>
    public sealed class LoadedMod
    {
        public ModManifest Manifest { get; internal set; }
        public IModHandle Handle { get; internal set; }
        public int Order { get; internal set; }
    }

    /* ---------- error-reporting DTOs ------------------------------ */
    public enum ErrorKind { MissingDependency, CircularDependency, IoError, ManifestError }

    public sealed class ModLoadError
    {
        public ErrorKind Kind;
        public string Message;
        public string[] InvolvedModIds;
    }

    /* ---------- persisted state table ----------------------------- */
    [Serializable]
    public sealed class ModStateTable
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public bool enabled;
            public int order;
        }

        public List<Entry> mods = new();
    }
}
