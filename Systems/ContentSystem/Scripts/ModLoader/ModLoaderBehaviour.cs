using System.IO;
using JG.GameContent.Debugging;
using JG.GameContent.Diagnostics;
using UnityEngine;

namespace JG.Modding
{
    /// <summary>Boots a <see cref="ModLoader"/> at runtime and exposes it to the UI.</summary>
    [AddComponentMenu("JG/Modding/Mod Loader")]
    [DefaultExecutionOrder(-100)]
    public sealed class ModLoaderBehaviour : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string modsRoot = "Mods";

        [Header("Importer")]
        [Tooltip("A MonoBehaviour that implements IContentImporter.")]
        [SerializeField] InterfaceReference<IContentImporter> importer;
        public ModLoader Loader { get; private set; }

        public bool InitialLoadDone { get; private set; }

        void Awake()
        {
            Debug.Log("ModLoaderBehaviour Awake");
            var cfg = new ModLoaderConfig { modsRoot = modsRoot };

            string projectOrBuildRoot =
                Directory.GetParent(Application.dataPath)!.FullName;
            var source = new FolderModSource(Path.Combine(projectOrBuildRoot, cfg.modsRoot));

            var manifest = new JsonManifestReader();
            var state = new JsonStateStore(Application.persistentDataPath, cfg.stateFile);

            Loader = new ModLoader(cfg, source, manifest, state, importer.Value,
#if !UNITY_IOS
                assemblyLoader: new ModAssemblyLoader(),
#endif
                loadInstantly: false);
            Loader.OnLoadError += e =>
            {
                var severity = e.Kind == ErrorKind.CircularDependency
                               || e.Kind == ErrorKind.MissingDependency
                               || e.Kind == ErrorKind.ManifestError
                               ? NotificationSeverity.Warning
                               : NotificationSeverity.Error;

                NotificationSender.Raise(e.Message, severity, "ModLoader");
            };
            Loader.OnDiagnosticsReady += report =>
            {
                if (report.ErrorCount > 0 || report.WarningCount > 0)
                {
                    var modSet = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                    foreach (var d in report.All)
                        if (!string.IsNullOrEmpty(d.ModId))
                            modSet.Add(d.ModId);
                    var modCount = modSet.Count;

                    NotificationSender.Raise(
                        $"Mod loading complete: {report.ErrorCount} error(s), {report.WarningCount} warning(s) across {modCount} mod(s).",
                        report.HasErrors ? NotificationSeverity.Error : NotificationSeverity.Warning,
                        "ModLoader");
                }
            };
            Loader.OnReloadFinished += () =>
            {
                if (!InitialLoadDone)
                    InitialLoadDone = true;
                Debug.Log("ModLoaderBehaviour: Mod loading finished.");
                EventBus<OnModLoadingFinishedEvent>.Raise(new OnModLoadingFinishedEvent());
            };

            // Bind debug console if present
            var console = Object.FindObjectOfType<DebugConsolePanel>();
            if (console != null)
                console.BindModLoader(Loader);

            Loader.Reload();

            Debug.Log($"ModLoader initialised, modsRoot = {modsRoot}, mods count: {Loader.ActiveMods.Count}");
            NotificationSender.Raise($"ModLoader: {Loader.ActiveMods.Count} Mods loaded.", NotificationSeverity.Info, "ModLoader");
        }

    }

    public struct OnModLoadingFinishedEvent : IEvent
    {

    }
}
