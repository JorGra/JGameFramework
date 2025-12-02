namespace JG.Modding
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;

    /* ----- Disk folder as ModSource ---------------------------------- */
    public sealed class FolderModSource : IModSource
    {
        readonly string _root;
        public FolderModSource(string root) => _root = root;

        public IEnumerable<IModHandle> Discover()
        {
            if (!Directory.Exists(_root))
                yield break;

            foreach (var dir in Directory.GetDirectories(_root))
                if (File.Exists(Path.Combine(dir, "manifest.json")))
                    yield return new FolderHandle(dir);
        }

        /* --- FolderHandle -------------------------------------------- */
        private sealed class FolderHandle : IModHandle
        {
            public string Path { get; }
            public FolderHandle(string dir) => Path = dir;

            public Stream OpenFile(string rel)
            {
                var fp = System.IO.Path.Combine(Path, rel);
                return File.Exists(fp) ? File.OpenRead(fp) : null;
            }

            public override string ToString() => Path;
        }
    }

    /* ----- JSON manifest --------------------------------------------- */
    public sealed class JsonManifestReader : IManifestReader
    {
        public ModManifest ReadManifest(IModHandle h)
        {
            using var s = h.OpenFile("manifest.json")
                 ?? throw new IOException($"manifest.json missing in {h}");
            using var sr = new StreamReader(s);
            return JsonConvert.DeserializeObject<ModManifest>(sr.ReadToEnd())
                   ?? throw new IOException($"Failed to parse manifest in {h}");
        }
    }

    /* ----- JSON state persistence ------------------------------------ */
    public sealed class JsonStateStore : IStateStore
    {
        readonly string _filePath;
        public JsonStateStore(string persistentDir, string fileName)
            => _filePath = System.IO.Path.Combine(persistentDir, fileName);

        public ModStateTable Load()
        {
            return File.Exists(_filePath)
                 ? JsonConvert.DeserializeObject<ModStateTable>(File.ReadAllText(_filePath))
                 : new ModStateTable();
        }

        public void Save(ModStateTable table)
            => File.WriteAllText(_filePath,
                                 JsonConvert.SerializeObject(table, Formatting.Indented));
    }
}
