using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JG.Modding.UI
{
    /// <summary>Drives the list & detail panel; now also hosts Apply/Revert.</summary>
    public sealed class ModListUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ModLoaderBehaviour modLoaderBehaviour;
        [SerializeField] private ModRowUI rowPrefab;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private ModDetailPanelUI detailPanel;

        [Header("Apply / Revert")]
        [SerializeField] private Button buttonApply;
        [SerializeField] private Button buttonRevert;

        readonly List<ModRowUI> rows = new();
        public int IndexOf(ModRowUI row) => rows.IndexOf(row);

        readonly Dictionary<string, string> modErrors = new();
        Action<ModLoadError> errorHandler;
        ModRowUI selectedRow;

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
            buttonApply.onClick.AddListener(OnApply);
            buttonRevert.onClick.AddListener(OnRevert);

            Refresh();
        }

        void OnDisable()
        {
            if (modLoaderBehaviour.Loader != null)
                modLoaderBehaviour.Loader.OnLoadError -= errorHandler;
        }

        public void OnRowSelected(ModRowUI row)
        {
            selectedRow = row;
            detailPanel.Show(row.LoadedMod,
                             modLoaderBehaviour.Loader.IsModEnabled(row.LoadedMod.Manifest.id),
                             row.ErrorMessage);
        }

        public void OnRowMoved() => UpdateRowInteractability();

        void OnApply()
        {
            modLoaderBehaviour.Loader.CommitChanges();
            modErrors.Clear();
            Refresh();
        }

        void OnRevert()
        {
            modLoaderBehaviour.Loader.RevertChanges();
            modErrors.Clear();
            Refresh();
        }

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

            if (selectedRow != null)
            {
                string id = selectedRow.LoadedMod.Manifest.id;
                var newSel = rows.Find(r => r.LoadedMod.Manifest.id == id);
                if (newSel != null) OnRowSelected(newSel);
            }
        }

        void UpdateRowInteractability()
        {
            for (int i = 0; i < rows.Count; i++)
                rows[i].SetMoveButtonsInteractable(i > 0, i < rows.Count - 1);
        }
    }
}
