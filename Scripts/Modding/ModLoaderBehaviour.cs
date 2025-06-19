using System.IO;
using UnityEngine;

namespace JG.Modding
{
    /// <summary>Boots a <see cref="ModLoader"/> at runtime and exposes it to the UI.</summary>
    [AddComponentMenu("JG/Modding/Mod Loader")]
    public sealed class ModLoaderBehaviour : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string modsRoot = "Mods";

        [Header("Importer")]
        [Tooltip("A MonoBehaviour that implements IContentImporter.")]
        [SerializeField] InterfaceReference<IContentImporter> importer;
        public ModLoader Loader { get; private set; }

        void Awake()
        {

            var cfg = new ModLoaderConfig { modsRoot = modsRoot };

            string projectOrBuildRoot =
                Directory.GetParent(Application.dataPath)!.FullName;
            var source = new FolderModSource(Path.Combine(projectOrBuildRoot, cfg.modsRoot));

            var manifest = new JsonManifestReader();
            var state = new JsonStateStore(Application.persistentDataPath, cfg.stateFile);

            Loader = new ModLoader(cfg, source, manifest, state, importer.Value);
            Loader.OnLoadError += e => NotificationSender.Raise(e.Message, NotificationSeverity.Error, "ModLoader");

            Debug.Log($"ModLoader initialised, modsRoot = {modsRoot}, mods count: {Loader.ActiveMods.Count}");
            NotificationSender.Raise($"ModLoader: {Loader.ActiveMods.Count} Mods loaded.", NotificationSeverity.Info, "ModLoader");
        }
    }
}
