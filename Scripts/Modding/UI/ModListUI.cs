using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Modding.UI
{
    /// <summary>
    /// Orchestrates the mod list & detail panel: recreates rows whenever the
    /// loader reloads or an error occurs, and routes selection to
    /// <see cref="ModDetailPanelUI"/>.
    /// </summary>
    public sealed class ModListUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ModLoaderBehaviour modLoaderBehaviour;
        [SerializeField] private ModRowUI rowPrefab;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private ModDetailPanelUI detailPanel;   // NEW

        readonly List<ModRowUI> rows = new();
        public int IndexOf(ModRowUI row) => rows.IndexOf(row);

        readonly Dictionary<string, string> modErrors = new();
        Action<ModLoadError> errorHandler;
        ModRowUI selectedRow;

        /* ---------- lifecycle -------------------------------------- */
        void Start()
        {
            errorHandler = err =>
            {
                if (err?.InvolvedModIds != null)
                    foreach (var id in err.InvolvedModIds)
                        modErrors[id] = err.Message;
                Refresh();
            };

            modLoaderBehaviour.Loader.OnLoadError += errorHandler;
            Refresh();
        }

        void OnDisable()
        {
            if (modLoaderBehaviour.Loader != null)
                modLoaderBehaviour.Loader.OnLoadError -= errorHandler;
        }

        /* ---------- public API for rows ----------------------------- */
        public void OnRowSelected(ModRowUI row)
        {
            selectedRow = row;
            detailPanel.Show(row.LoadedMod,
                             modLoaderBehaviour.Loader.IsModEnabled(row.LoadedMod.Manifest.id),
                             row.ErrorMessage);
        }

        public void OnRowMoved() => UpdateRowInteractability();

        /* ---------- main refresh ----------------------------------- */
        public void Refresh()
        {
            foreach (var r in rows) Destroy(r.gameObject);
            rows.Clear();

            var loader = modLoaderBehaviour.Loader;
            if (loader == null || loader.ActiveMods == null) return;

            foreach (var lm in loader.ActiveMods)
            {
                var row = Instantiate(rowPrefab, contentRoot);
                modErrors.TryGetValue(lm.Manifest.id, out var err);
                row.Init(lm, loader, this, err);
                rows.Add(row);
            }

            UpdateRowInteractability();

            // keep detail panel in sync if the selected row got recreated
            if (selectedRow != null)
            {
                var id = selectedRow.LoadedMod.Manifest.id;
                var newSel = rows.Find(r => r.LoadedMod.Manifest.id == id);
                if (newSel != null) OnRowSelected(newSel);
            }
        }

        /* ---------- helpers ---------------------------------------- */
        void UpdateRowInteractability()
        {
            for (int i = 0; i < rows.Count; i++)
                rows[i].SetMoveButtonsInteractable(i > 0, i < rows.Count - 1);
        }
    }
}
