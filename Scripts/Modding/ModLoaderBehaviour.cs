using System;
using System.IO;
using UnityEngine;

namespace JG.Modding
{
    /// <summary>
    /// Boots a <see cref="ModLoader"/> at runtime and exposes it to the UI.
    /// </summary>
    [AddComponentMenu("JG/Modding/Mod Loader")]
    public sealed class ModLoaderBehaviour : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string modsRoot = "Mods";
        [SerializeField] private bool fullReloadOnChange = true;

        [Header("Importer")]
        [Tooltip("A MonoBehaviour that implements IContentImporter.")]
        [SerializeField] private MonoBehaviour importerBehaviour;

        /// <summary>The running <see cref="ModLoader"/> instance.</summary>
        public ModLoader Loader { get; private set; }

        void Awake()
        {
            if (importerBehaviour is not IContentImporter importer)
            {
                Debug.LogError($"{importerBehaviour.name} must implement IContentImporter");
                //enabled = false;
                return;
            }

            var cfg = new ModLoaderConfig
            {
                modsRoot = modsRoot,
                fullReloadOnChange = fullReloadOnChange
            };

            var source = new FolderModSource(Path.Combine(AppContext.BaseDirectory, cfg.modsRoot));
            var manifest = new JsonManifestReader();
            var state = new JsonStateStore(Application.persistentDataPath, cfg.stateFile);

            Loader = new ModLoader(cfg, source, manifest, state, importer);
            Loader.OnLoadError += e => Debug.LogError(e.Message);
        }
    }
}
