using JG.GameContent;
using JG.GameContent.Diagnostics;
using JG.GameContent.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
#if !UNITY_IOS
using System.Reflection;
#endif

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

#if !UNITY_IOS
        readonly IModAssemblyLoader _assemblyLoader;
        readonly List<Assembly> _modAssemblies = new();
        readonly Dictionary<Assembly, LoadedMod> _assemblyToMod = new();
#endif

        readonly List<LoadedMod> _mods = new();
        ModStateTable _state;

        /// <summary>Current working list in UI order (updated live, not yet imported).</summary>
        public IReadOnlyList<LoadedMod> ActiveMods { get; private set; } = Array.Empty<LoadedMod>();

        bool _dirty;                                   // true if edits not yet applied
        public event Action<ModLoadError> OnLoadError;
        public event Action OnReloadFinished;

        /// <summary>
        /// Fired after content import but before mod entry points run.
        /// The host game should subscribe to this to populate <see cref="ServiceRegistry"/>
        /// with game-specific services (singletons, registries, etc.).
        /// </summary>
        public event Action OnBeforeEntryPoints;

        /// <summary>
        /// Service registry that the host game populates (typically via <see cref="OnBeforeEntryPoints"/>).
        /// Passed to every <see cref="IModContext.Services"/> so mods can resolve game-specific services.
        /// </summary>
        public ModServiceRegistry ServiceRegistry { get; } = new();

        public DiagnosticReport Diagnostics { get; private set; }
        public event Action<DiagnosticReport> OnDiagnosticsReady;
        public event Action<string> OnLoadProgress;

        /* ------------------------------------------------------------ */
        public ModLoader(ModLoaderConfig cfg,
                         IModSource source,
                         IManifestReader reader,
                         IStateStore stateStore,
                         IContentImporter importer,
#if !UNITY_IOS
                         IModAssemblyLoader assemblyLoader = null,
#endif
                         bool loadInstantly = false)
        {
            _cfg = cfg;
            _source = source;
            _reader = reader;
            _stateStore = stateStore;
            _importer = importer;
#if !UNITY_IOS
            _assemblyLoader = assemblyLoader;
#endif

            if(loadInstantly)
                Reload();
        }

        /* ------------------------------------------------------------ */
        public void Reload()
        {
            Diagnostics = new DiagnosticReport();

            _mods.Clear();
            _state = _stateStore.Load() ?? new ModStateTable();
            ActiveMods = Array.Empty<LoadedMod>();

            OnLoadProgress?.Invoke("Discovering mods...");
            DiscoverMods();
            OnLoadProgress?.Invoke($"Found {_mods.Count} mod(s).");

            OnLoadProgress?.Invoke("Resolving dependencies...");
            if (!ResolveOrder(out var ordered)) return;
            OnLoadProgress?.Invoke($"Load order resolved: {string.Join(", ", ordered.Select(m => m.Manifest.id))}");

            BuildStateTable(ordered);
            ActiveMods = ordered.AsReadOnly();

#if !UNITY_IOS
            // Load mod assemblies before content import so new types are discoverable
            LoadModAssemblies(ordered);

            // Reset reflection caches so newly loaded types are picked up
            ResetReflectionCaches();
#endif

            ContentCatalogue.Instance.Clear();
            ModTranslationLoader.Instance.Clear();

            // Wire diagnostic sink into importer if supported
            if (_importer is JsonContentImporter jsonImporter)
            {
                jsonImporter.DiagnosticSink = Diagnostics;
                jsonImporter.OnProgress = msg => OnLoadProgress?.Invoke(msg);
            }

            foreach (var mod in ordered)
            {
                var row = _state.mods.First(e => e.id == mod.Manifest.id);
                if (!row.enabled) continue;

                OnLoadProgress?.Invoke($"Importing content for {mod.Manifest.id}...");
                try
                {
                    _importer.Import(mod.Handle);
                }
                catch (Exception ex)
                {
                    Raise(ErrorKind.IoError,
                          $"Import failed for {mod.Manifest.id}: {ex.Message}",
                          new[] { mod.Manifest.id });
                }
            }

#if !UNITY_IOS
            // Let the host game populate the service registry before entry points run
            ServiceRegistry.Clear();
            OnBeforeEntryPoints?.Invoke();

            // Run mod entry points after content import so mods can query the catalogue
            RunModEntryPoints(ordered);
#endif

            OnLoadProgress?.Invoke("Running validation...");
            ContentValidationRunner.RunAll(ContentCatalogue.Instance, Diagnostics);

            OnLoadProgress?.Invoke($"Loading complete. {Diagnostics.ErrorCount} error(s), {Diagnostics.WarningCount} warning(s).");
            OnDiagnosticsReady?.Invoke(Diagnostics);
            OnReloadFinished?.Invoke();
            _dirty = false;
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

        public DiagnosticReport ValidateNow()
        {
            var report = new DiagnosticReport();
            ContentValidationRunner.RunAll(ContentCatalogue.Instance, report);
            return report;
        }

        /* ---------- DLL loading internals --------------------------- */
#if !UNITY_IOS
        void LoadModAssemblies(List<LoadedMod> ordered)
        {
            _modAssemblies.Clear();
            _assemblyToMod.Clear();

            if (_assemblyLoader == null)
                return;

            foreach (var mod in ordered)
            {
                var row = _state.mods.First(e => e.id == mod.Manifest.id);
                if (!row.enabled) continue;

                if (mod.Manifest.assemblies == null || mod.Manifest.assemblies.Length == 0)
                    continue;

                try
                {
                    var loaded = _assemblyLoader.LoadAssemblies(mod.Handle, mod.Manifest);
                    foreach (var asm in loaded)
                    {
                        _modAssemblies.Add(asm);
                        _assemblyToMod[asm] = mod;
                    }
                }
                catch (Exception ex)
                {
                    Raise(ErrorKind.IoError,
                          $"Assembly loading failed for {mod.Manifest.id}: {ex.Message}",
                          new[] { mod.Manifest.id });
                }
            }
        }

        static void ResetReflectionCaches()
        {
            JsonContentImporter.RescanContentTypes();
            DiscriminatorConverterRegistry.ResetAll();
        }

        void RunModEntryPoints(List<LoadedMod> ordered)
        {
            if (_modAssemblies.Count == 0)
                return;

            foreach (var asm in _modAssemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }
                catch { continue; }

                if (types == null) continue;

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface)
                        continue;
                    if (!typeof(IModEntryPoint).IsAssignableFrom(type))
                        continue;

                    try
                    {
                        var entryPoint = (IModEntryPoint)Activator.CreateInstance(type);
                        var mod = _assemblyToMod.TryGetValue(asm, out var m) ? m : null;
                        var context = new ModContext(
                            mod?.Manifest.id ?? "unknown",
                            mod?.Handle.Path ?? string.Empty,
                            ServiceRegistry
                        );
                        entryPoint.Initialize(context);
                        UnityEngine.Debug.Log($"[ModLoader] Initialized entry point {type.FullName} for mod {context.ModId}");
                    }
                    catch (Exception ex)
                    {
                        var modId = _assemblyToMod.TryGetValue(asm, out var m2) ? m2.Manifest.id : "unknown";
                        UnityEngine.Debug.LogError($"[ModLoader] Entry point {type.FullName} failed (mod: {modId}): {ex}");
                        Diagnostics?.Report(new ContentDiagnostic
                        {
                            Severity = DiagnosticSeverity.Error,
                            Category = DiagnosticCategory.Assembly,
                            ModId = modId,
                            Message = $"Entry point {type.FullName} failed: {ex.Message}",
                            Detail = ex.InnerException?.Message
                        });
                    }
                }
            }
        }
#endif

        /* ---------- internals --------------------------------------- */
        void DiscoverMods()
        {
            foreach (var h in _source.Discover())
            {
                try { _mods.Add(new LoadedMod { Manifest = _reader.ReadManifest(h), Handle = h }); }
                catch (Exception ex)
                {
                    Raise(ErrorKind.ManifestError, ex.Message, null);
                }
            }
        }

        bool ResolveOrder(out List<LoadedMod> ordered)
        {
            // Work on a mutable copy so we can delete broken entries.
            var work = new List<LoadedMod>(_mods);
            bool removed;

            // ── Pass 1: throw out mods whose *requires* point to something absent ───────
            do
            {
                removed = false;
                var ids = work.Select(m => m.Manifest.id).ToHashSet();

                foreach (var m in work.ToArray())                      // copy because we mutate
                    if (m.Manifest.requires.Any(r => !ids.Contains(r)))
                    {
                        Raise(ErrorKind.MissingDependency,
                              $"Skipping {m.Manifest.id}: missing dependency.",
                              new[] { m.Manifest.id });
                        work.Remove(m);                                // ► skip, don’t abort
                        removed = true;
                    }
            } while (removed);                                         // keep trimming layers

            // ── Pass 2: simply ignore dangling loadBefore / loadAfter edges ────────────
            var liveIds = work.Select(m => m.Manifest.id).ToHashSet();
            foreach (var m in work)
            {
                m.Manifest.loadBefore = m.Manifest.loadBefore.Where(liveIds.Contains).ToArray();
                m.Manifest.loadAfter = m.Manifest.loadAfter.Where(liveIds.Contains).ToArray();
            }

            // ── Normal topological sort now succeeds for the surviving mods ────────────
            if (!TopologicalSorter.TrySort(work, out ordered, out var cycles))
            {
                Raise(ErrorKind.CircularDependency,
                      "Cyclic dependency: " +
                      string.Join(", ", cycles.Select(c => $"{c.a}->{c.b}")),
                      cycles.SelectMany(c => new[] { c.a, c.b }).Distinct().ToArray());

                // Remove the entire strongly-connected set and try again
                work.RemoveAll(m => cycles.Any(c => c.a == m.Manifest.id || c.b == m.Manifest.id));
                return TopologicalSorter.TrySort(work, out ordered, out _) && ordered.Count > 0;
            }

            for (int i = 0; i < ordered.Count; i++) ordered[i].Order = i;
            return true;                                               // never stops the loader
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
        {
            OnLoadError?.Invoke(new ModLoadError { Kind = k, Message = msg, InvolvedModIds = mods });
            Diagnostics?.Report(new ContentDiagnostic
            {
                Severity = k == ErrorKind.IoError ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                Category = k switch
                {
                    ErrorKind.ManifestError => DiagnosticCategory.Manifest,
                    ErrorKind.MissingDependency => DiagnosticCategory.Dependency,
                    ErrorKind.CircularDependency => DiagnosticCategory.Dependency,
                    _ => DiagnosticCategory.Custom
                },
                ModId = mods != null && mods.Length > 0 ? mods[0] : null,
                Message = msg
            });
        }
    }
}
