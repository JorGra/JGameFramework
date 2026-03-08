using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.GameContent.Debugging
{
    public sealed class DebugCommandPanel : UIPanelAnimated
    {
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform contentParent;
        [SerializeField] GameObject commandButtonPrefab;
        [SerializeField] GameObject categoryHeaderPrefab;
        [SerializeField] Button closeButton;

        private readonly List<GameObject> _spawnedItems = new();
        LayoutElement _layoutElement;

        protected override void Awake()
        {
            base.Awake();
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement != null) _layoutElement.ignoreLayout = true;
            DebugCommandRegistry.InitializeDefaults();
            DebugCommandRegistry.OnCommandsChanged += OnCommandsChanged;

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        public override void Open()
        {
            if (_layoutElement != null) _layoutElement.ignoreLayout = false;
            base.Open();
            RebuildButtons();
        }

        public override void Close()
        {
            base.Close();
            if (_layoutElement != null) _layoutElement.ignoreLayout = true;
        }

        private void OnCommandsChanged()
        {
            if (IsOpen)
                RebuildButtons();
        }

        private void RebuildButtons()
        {
            foreach (var item in _spawnedItems)
                Destroy(item);
            _spawnedItems.Clear();

            var grouped = DebugCommandRegistry.Commands
                .GroupBy(c => c.Category)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                // Category header
                if (categoryHeaderPrefab != null)
                {
                    var header = Instantiate(categoryHeaderPrefab, contentParent);
                    var headerText = header.GetComponentInChildren<TMP_Text>();
                    if (headerText != null)
                        headerText.text = group.Key;
                    _spawnedItems.Add(header);
                }

                // Command buttons
                foreach (var command in group)
                {
                    if (commandButtonPrefab == null) continue;

                    var buttonObj = Instantiate(commandButtonPrefab, contentParent);
                    var label = buttonObj.GetComponentInChildren<TMP_Text>();
                    if (label != null)
                        label.text = command.Name;

                    var button = buttonObj.GetComponent<Button>();
                    if (button != null)
                    {
                        var callback = command.Callback;
                        button.onClick.AddListener(() => callback?.Invoke());
                    }

                    _spawnedItems.Add(buttonObj);
                }
            }
        }

        private void OnDestroy()
        {
            DebugCommandRegistry.OnCommandsChanged -= OnCommandsChanged;
        }
    }
}
