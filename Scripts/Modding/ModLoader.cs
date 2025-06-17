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

        /// <summary>Current working list in UI order (updated live, not yet imported).</summary>
        public IReadOnlyList<LoadedMod> ActiveMods { get; private set; } = Array.Empty<LoadedMod>();

        bool _dirty;                                   // true if edits not yet applied
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

            Reload();                                  // prime everything from disk
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
            ActiveMods = ordered.AsReadOnly();

            foreach (var mod in ordered)
                if (_state.mods.First(r => r.id == mod.Manifest.id).enabled)
                    _importer.Import(mod.Handle);

            _dirty = false;                            // disk state now matches memory
        }

        /* ---------- live editing helpers ---------------------------- */
        public void Enable(string id, bool on)
        {
            var row = _state.mods.FirstOrDefault(e => e.id == id);
            if (row == null) return;

            row.enabled = on;
            _dirty = true;
        }

        /// <summary>Re-order a mod in the current list, validating constraints.</summary>
        public bool Move(string id, int newPos)
        {
            var list = _state.mods;
            int idx = list.FindIndex(e => e.id == id);
            if (idx == -1 || newPos < 0 || newPos >= list.Count) return false;

            var entry = list[idx];
            list.RemoveAt(idx);
            list.Insert(newPos, entry);
            for (int i = 0; i < list.Count; i++) list[i].order = i;

            if (!ValidateCurrentOrder(out var err))
            {
                // undo move and report
                list.RemoveAt(newPos);
                list.Insert(idx, entry);
                for (int i = 0; i < list.Count; i++) list[i].order = i;
                Raise(ErrorKind.CircularDependency, err, null);
                return false;
            }

            ActiveMods = _mods
                .OrderBy(m => list.Find(e => e.id == m.Manifest.id).order)
                .ToList()
                .AsReadOnly();

            _dirty = true;
            return true;
        }

        /// <summary>Write the working state to disk and perform a full import.</summary>
        public void CommitChanges()
        {
            if (!_dirty) return;
            _stateStore.Save(_state);
            Reload();
        }

        /// <summary>Discard all un-saved edits and reload the persisted state.</summary>
        public void RevertChanges()
        {
            if (!_dirty) return;
            Reload();
        }

        public bool IsModEnabled(string id)
            => _state?.mods.FirstOrDefault(e => e.id == id)?.enabled ?? false;

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
                prev.TryGetValue(m.Manifest.id, out var old)
                    ? new ModStateTable.Entry { id = m.Manifest.id, enabled = old.enabled, order = idx }
                    : new ModStateTable.Entry { id = m.Manifest.id, enabled = true, order = idx })
                .ToList();
        }

        bool ValidateCurrentOrder(out string err)
        {
            var order = _state.mods.ToDictionary(e => e.id, e => e.order);
            foreach (var m in _mods)
            {
                foreach (var b in m.Manifest.loadBefore)
                    if (order[m.Manifest.id] >= order[b])
                    {
                        err = $"{m.Manifest.id} must come before {b}";
                        return false;
                    }
                foreach (var a in m.Manifest.loadAfter)
                    if (order[a] >= order[m.Manifest.id])
                    {
                        err = $"{m.Manifest.id} must come after {a}";
                        return false;
                    }
            }
            err = null;
            return true;
        }

        void Raise(ErrorKind k, string msg, string[] mods)
            => OnLoadError?.Invoke(new ModLoadError { Kind = k, Message = msg, InvolvedModIds = mods });
    }
}
