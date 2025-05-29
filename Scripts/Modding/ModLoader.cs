using System;
using System.Collections.Generic;
using System.Linq;

namespace JG.Modding
{
    /// <summary>Discovers, validates, sorts, and imports all enabled mods.</summary>
    public sealed class ModLoader
    {
        readonly ModLoaderConfig _cfg;
        readonly IModSource _source;
        readonly IManifestReader _reader;
        readonly IStateStore _stateStore;
        readonly IContentImporter _importer;

        readonly List<LoadedMod> _mods = new();
        ModStateTable _state;

        public IReadOnlyList<LoadedMod> ActiveMods { get; private set; }

        public event Action<ModLoadError> OnLoadError;

        /* ------------------------------------------------------------ */
        public ModLoader(ModLoaderConfig cfg,
                         IModSource source,
                         IManifestReader reader,
                         IStateStore stateStore,
                         IContentImporter importer)
        {
            _cfg = cfg;
            _source = source;
            _reader = reader;
            _stateStore = stateStore;
            _importer = importer;

            Reload();
        }

        /* ------------------------------------------------------------ */
        public void Reload()
        {
            _mods.Clear();
            _state = _stateStore.Load() ?? new ModStateTable();
            ActiveMods = Array.Empty<LoadedMod>();

            DiscoverMods();
            if (!ResolveOrder(out var ordered)) return;

            BuildStateTable(ordered);
            _stateStore.Save(_state);

            ActiveMods = ordered.AsReadOnly();

            foreach (var mod in ordered)
                if (_state.mods.First(r => r.id == mod.Manifest.id).enabled)
                    _importer.Import(mod.Handle);
        }

        /* ---------- UI helpers -------------------------------------- */
        public void Enable(string id, bool on)
        {
            var row = _state.mods.FirstOrDefault(e => e.id == id);
            if (row == null) return;

            row.enabled = on;
            _stateStore.Save(_state);
            if (_cfg.fullReloadOnChange) Reload();
        }

        public void Move(string id, int newPos)
        {
            var list = _state.mods;
            int idx = list.FindIndex(e => e.id == id);
            if (idx == -1 || newPos < 0 || newPos >= list.Count) return;

            var entry = list[idx];
            list.RemoveAt(idx);
            list.Insert(newPos, entry);

            for (int i = 0; i < list.Count; i++) list[i].order = i;

            _stateStore.Save(_state);
            if (_cfg.fullReloadOnChange) Reload();
        }

        /* ---------- internals --------------------------------------- */
        void DiscoverMods()
        {
            foreach (var h in _source.Discover())
            {
                try { _mods.Add(new LoadedMod { Manifest = _reader.ReadManifest(h), Handle = h }); }
                catch (Exception ex) { Raise(ErrorKind.ManifestError, ex.Message, null); }
            }
        }

        bool ResolveOrder(out List<LoadedMod> ordered)
        {
            ordered = null;

            /* 1) hard + soft dependency validation ------------------- */
            var ids = _mods.Select(m => m.Manifest.id).ToHashSet();

            foreach (var m in _mods)
            {
                foreach (var req in m.Manifest.requires)
                    if (!ids.Contains(req))
                    {
                        Raise(ErrorKind.MissingDependency,
                              $"{m.Manifest.id} requires missing mod {req}",
                              new[] { m.Manifest.id, req });
                        return false;
                    }

                foreach (var tgt in m.Manifest.loadBefore.Concat(m.Manifest.loadAfter))
                    if (!ids.Contains(tgt))
                    {
                        Raise(ErrorKind.MissingDependency,
                              $"{m.Manifest.id} references non-existent mod {tgt}",
                              new[] { m.Manifest.id, tgt });
                        return false;
                    }
            }

            /* 2) seed order from previous state ---------------------- */
            var id2Mod = _mods.ToDictionary(m => m.Manifest.id);

            var list = _state.mods
                             .Where(r => id2Mod.ContainsKey(r.id))
                             .OrderBy(r => r.order)
                             .Select(r => id2Mod[r.id])
                             .ToList();

            list.AddRange(_mods.Where(m => list.All(o => o.Manifest.id != m.Manifest.id)));

            /* 3) topo-sort ------------------------------------------- */
            if (!TopologicalSorter.TrySort(list, out var topo, out var cycles))
            {
                Raise(ErrorKind.CircularDependency,
                      "Cycle in loadBefore/loadAfter: " +
                      string.Join(", ", cycles.Select(c => $"{c.a}->{c.b}")),
                      cycles.SelectMany(c => new[] { c.a, c.b }).Distinct().ToArray());
                return false;
            }

            for (int i = 0; i < topo.Count; i++) topo[i].Order = i;

            ordered = topo;
            return true;
        }

        void BuildStateTable(List<LoadedMod> ordered)
        {
            var prev = _state.mods.ToDictionary(e => e.id);

            _state.mods = ordered.Select((m, idx) =>
            {
                return prev.TryGetValue(m.Manifest.id, out var old)
                     ? new ModStateTable.Entry { id = m.Manifest.id, enabled = old.enabled, order = idx }
                     : new ModStateTable.Entry { id = m.Manifest.id, enabled = true, order = idx };
            }).ToList();
        }

        /// <summary>Returns the current enabled flag for the given mod ID.</summary>
        public bool IsModEnabled(string id)
        {
            var row = _state?.mods.FirstOrDefault(e => e.id == id);
            return row?.enabled ?? false;
        }

        void Raise(ErrorKind k, string msg, string[] mods)
            => OnLoadError?.Invoke(new ModLoadError { Kind = k, Message = msg, InvolvedModIds = mods });
    }
}
