using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Modding.UI
{
    public sealed class ModListUI : MonoBehaviour
    {
        [SerializeField] private ModLoaderBehaviour modLoaderBehaviour;
        [SerializeField] private ModRowUI rowPrefab;
        [SerializeField] private Transform contentRoot;

        readonly List<ModRowUI> rows = new();
        Action<ModLoadError> errorHandler;

        void Start()
        {
            errorHandler = _ => Refresh();
            modLoaderBehaviour.Loader.OnLoadError += errorHandler;
            Refresh();
        }

        void OnDisable()
        {
            if (modLoaderBehaviour.Loader != null)
                modLoaderBehaviour.Loader.OnLoadError -= errorHandler;
        }

        public void Refresh()
        {
            foreach (var r in rows) Destroy(r.gameObject);
            rows.Clear();

            var loader = modLoaderBehaviour.Loader;
            if (loader == null) return;

            var mods = loader.ActiveMods;
            if (mods == null || mods.Count == 0) return;

            foreach (var lm in mods)
            {
                var row = Instantiate(rowPrefab, contentRoot);
                row.Init(lm, loader, this);
                rows.Add(row);
            }

            UpdateRowInteractability();
        }

        public void OnRowMoved() => UpdateRowInteractability();

        void UpdateRowInteractability()
        {
            for (int i = 0; i < rows.Count; i++)
                rows[i].SetMoveButtonsInteractable(i > 0, i < rows.Count - 1);
        }
    }
}
