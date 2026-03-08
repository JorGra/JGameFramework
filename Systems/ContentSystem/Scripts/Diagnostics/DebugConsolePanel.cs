using System;
using System.Collections;
using System.Collections.Generic;
using JG.GameContent.Diagnostics;
using JG.Modding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.GameContent.Debugging
{
    public sealed class DebugConsolePanel : UIPanelAnimated
    {
        [Header("References")]
        [SerializeField] ConsoleLogCapture logCapture;
        [SerializeField] DebugConsoleStatusBar statusBar;

        [Header("Toolbar")]
        [SerializeField] Toggle errorToggle;
        [SerializeField] Toggle warningToggle;
        [SerializeField] Toggle infoToggle;
        [SerializeField] TMP_Text errorCountText;
        [SerializeField] TMP_Text warningCountText;
        [SerializeField] TMP_Text infoCountText;
        [SerializeField] TMP_InputField searchField;
        [SerializeField] Button clearButton;
        [SerializeField] Button copyButton;
        [SerializeField] Button pauseButton;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text pauseButtonText;
        [SerializeField] Button[] tabButtons;

        [Header("All Logs Tab")]
        [SerializeField] ScrollRect logScrollRect;
        [SerializeField] RectTransform logContent;
        [SerializeField] GameObject logEntryPrefab;
        [SerializeField] int maxVisibleEntries = 200;
        [SerializeField] Sprite errorIcon;
        [SerializeField] Sprite warningIcon;
        [SerializeField] Sprite infoIcon;

        [Header("Diagnostics Tab")]
        [SerializeField] RectTransform diagnosticsTab;
        [SerializeField] ScrollRect diagnosticsScrollRect;
        [SerializeField] RectTransform diagnosticsContent;
        [SerializeField] GameObject diagnosticsEntryPrefab;

        [Header("Loading Log Tab")]
        [SerializeField] RectTransform loadingLogTab;
        [SerializeField] ScrollRect loadingLogScrollRect;
        [SerializeField] TMP_Text loadingLogText;

        [Header("Behavior")]
        [SerializeField] bool autoOpenOnError = true;
        [SerializeField] KeyCode toggleKey = KeyCode.F11;

        // Log entry pool
        readonly List<DebugConsoleLogEntryView> _logEntryPool = new();
        readonly List<int> _filteredIndices = new();
        int _cachedVersion = -1;
        int _expandedIndex = -1;
        bool _autoScrollEnabled = true;
        bool _hasAutoOpened;

        // Diagnostics
        DiagnosticReport _report;
        readonly List<TMP_Text> _diagEntryPool = new();

        // Loading log
        readonly List<string> _loadingEntries = new();

        // Tabs
        int _activeTab;

        static readonly Color RowEven = new(0.18f, 0.18f, 0.18f, 0.95f);
        static readonly Color RowOdd = new(0.22f, 0.22f, 0.22f, 0.95f);

        protected override void Awake()
        {
            base.Awake();
            SetupButtons();
            SetupToggles();
            SetupTabs();

            if (logScrollRect != null)
                logScrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            // StatusBar starts hidden — F11 to show
            if (statusBar != null)
                statusBar.SetVisible(false);
        }

        void Update()
        {
            if (logCapture == null || logCapture.Buffer == null) return;

            UpdateBadgeCounts();

            // Auto-open on error: show the StatusBar so the user sees error counts
            if (autoOpenOnError && !_hasAutoOpened && statusBar != null && !statusBar.gameObject.activeSelf)
            {
                var buffer = logCapture.Buffer;
                int ver = buffer.Version;
                if (ver != _cachedVersion)
                {
                    int count = buffer.Count;
                    for (int i = Mathf.Max(0, count - 5); i < count; i++)
                    {
                        var entry = buffer.Get(i);
                        if (entry.Type == LogType.Error || entry.Type == LogType.Exception || entry.Type == LogType.Assert)
                        {
                            _hasAutoOpened = true;
                            statusBar.SetVisible(true);
                            break;
                        }
                    }
                }
            }

            if (!IsOpen) return;

            if (_activeTab == 0)
                RefreshLogView();
        }

        #region Public API

        public void BindModLoader(ModLoader loader)
        {
            if (loader == null) return;
            loader.OnLoadProgress += OnLoadProgress;
            loader.OnDiagnosticsReady += OnDiagnosticsReady;
        }

        public void ResetAutoOpen()
        {
            _hasAutoOpened = false;
        }

        public override void Open()
        {
            base.Open();
            // Delay the refresh by one frame so Unity's UI (toggles, layout) has settled
            StartCoroutine(DelayedRefresh());
        }

        IEnumerator DelayedRefresh()
        {
            yield return null;
            InvalidateFilter();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        #endregion

        #region Key Toggle

        void OnGUI()
        {
            if (toggleKey == KeyCode.None) return;
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == toggleKey)
            {
                // F11 toggles the StatusBar (dev mode on/off)
                if (statusBar != null)
                {
                    bool isVisible = statusBar.gameObject.activeSelf;
                    statusBar.SetVisible(!isVisible);
                }
                e.Use();
            }
        }

        #endregion

        #region Button Setup

        void SetupButtons()
        {
            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearClicked);
            if (copyButton != null)
                copyButton.onClick.AddListener(OnCopyClicked);
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(() => Close());
        }

        void SetupToggles()
        {
            if (errorToggle != null)
            {
                errorToggle.isOn = true;
                errorToggle.onValueChanged.AddListener(on => { InvalidateFilter(); UpdateToggleVisual(errorToggle, on); });
                UpdateToggleVisual(errorToggle, true);
            }
            if (warningToggle != null)
            {
                warningToggle.isOn = true;
                warningToggle.onValueChanged.AddListener(on => { InvalidateFilter(); UpdateToggleVisual(warningToggle, on); });
                UpdateToggleVisual(warningToggle, true);
            }
            if (infoToggle != null)
            {
                infoToggle.isOn = true;
                infoToggle.onValueChanged.AddListener(on => { InvalidateFilter(); UpdateToggleVisual(infoToggle, on); });
                UpdateToggleVisual(infoToggle, true);
            }
            if (searchField != null)
                searchField.onValueChanged.AddListener(_ => InvalidateFilter());
        }

        static void UpdateToggleVisual(Toggle toggle, bool isOn)
        {
            var img = toggle.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = isOn ? 0.4f : 0.08f;
                img.color = c;
            }
        }

        void SetupTabs()
        {
            if (tabButtons == null) return;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                tabButtons[i].onClick.AddListener(() => OnTabSelected(idx));
            }
            OnTabSelected(0);
        }

        #endregion

        #region Tabs

        void OnTabSelected(int index)
        {
            _activeTab = index;

            // Toggle tab content visibility
            if (logScrollRect != null) logScrollRect.gameObject.SetActive(index == 0);
            if (diagnosticsTab != null) diagnosticsTab.gameObject.SetActive(index == 1);
            if (loadingLogTab != null) loadingLogTab.gameObject.SetActive(index == 2);

            // Highlight active tab button
            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    var colors = tabButtons[i].colors;
                    colors.normalColor = i == index ? new Color(0.35f, 0.35f, 0.35f) : new Color(0.2f, 0.2f, 0.2f);
                    tabButtons[i].colors = colors;
                }
            }

            if (index == 0) InvalidateFilter();
            if (index == 1) RefreshDiagnosticsView();
            if (index == 2) RefreshLoadingLogView();
        }

        #endregion

        #region All Logs Tab

        void RefreshLogView()
        {
            var buffer = logCapture.Buffer;
            int version = buffer.Version;
            if (version == _cachedVersion) return;
            _cachedVersion = version;

            RebuildFilteredList();

            // Ensure pool is large enough
            int needed = Mathf.Min(_filteredIndices.Count, maxVisibleEntries);
            EnsurePoolSize(needed);

            int visibleCount = needed;
            int startIdx = Mathf.Max(0, _filteredIndices.Count - visibleCount);

            for (int i = 0; i < _logEntryPool.Count; i++)
            {
                if (i < visibleCount)
                {
                    var view = _logEntryPool[i];
                    view.gameObject.SetActive(true);

                    int dataIdx = startIdx + i;
                    int bufferIdx = _filteredIndices[dataIdx];
                    var entry = buffer.Get(bufferIdx);
                    var bgColor = (i % 2 == 0) ? RowEven : RowOdd;
                    view.Bind(entry, _expandedIndex == bufferIdx, bgColor);
                    view.transform.SetSiblingIndex(i);
                }
                else
                {
                    _logEntryPool[i].gameObject.SetActive(false);
                }
            }

            if (_autoScrollEnabled && logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        void RebuildFilteredList()
        {
            _filteredIndices.Clear();
            var buffer = logCapture.Buffer;
            int count = buffer.Count;
            bool showErrors = errorToggle == null || errorToggle.isOn;
            bool showWarnings = warningToggle == null || warningToggle.isOn;
            bool showInfo = infoToggle == null || infoToggle.isOn;
            string search = searchField != null ? searchField.text : "";

            for (int i = 0; i < count; i++)
            {
                var entry = buffer.Get(i);

                // Severity filter
                switch (entry.Type)
                {
                    case LogType.Error:
                    case LogType.Exception:
                    case LogType.Assert:
                        if (!showErrors) continue;
                        break;
                    case LogType.Warning:
                        if (!showWarnings) continue;
                        break;
                    default:
                        if (!showInfo) continue;
                        break;
                }

                // Search filter
                if (!string.IsNullOrEmpty(search))
                {
                    if (entry.Message == null ||
                        entry.Message.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }

                _filteredIndices.Add(i);
            }
        }

        void EnsurePoolSize(int needed)
        {
            while (_logEntryPool.Count < needed)
            {
                if (logEntryPrefab == null || logContent == null) break;
                var go = Instantiate(logEntryPrefab, logContent);
                var view = go.GetComponent<DebugConsoleLogEntryView>();
                if (view == null)
                {
                    Destroy(go);
                    break;
                }
                view.SetIcons(errorIcon, warningIcon, infoIcon);
                int capturedIdx = _logEntryPool.Count;
                view.SetExpandCallback(_ => OnLogEntryClicked(capturedIdx));
                _logEntryPool.Add(view);
            }
        }

        void OnLogEntryClicked(int poolIndex)
        {
            int visibleCount = Mathf.Min(_filteredIndices.Count, maxVisibleEntries);
            int startIdx = Mathf.Max(0, _filteredIndices.Count - visibleCount);
            int dataIdx = startIdx + poolIndex;

            if (dataIdx < 0 || dataIdx >= _filteredIndices.Count) return;

            int bufferIdx = _filteredIndices[dataIdx];
            if (_expandedIndex == bufferIdx)
            {
                _expandedIndex = -1;
                if (poolIndex < _logEntryPool.Count)
                    _logEntryPool[poolIndex].SetExpanded(false);
            }
            else
            {
                _expandedIndex = bufferIdx;
                _cachedVersion = -1; // Force refresh to update expand states
            }
        }

        void OnScrollValueChanged(Vector2 pos)
        {
            _autoScrollEnabled = pos.y <= 0.01f;
        }

        void InvalidateFilter()
        {
            _cachedVersion = -1;
        }

        #endregion

        #region Diagnostics Tab

        void RefreshDiagnosticsView()
        {
            if (diagnosticsContent == null) return;

            // Clear existing
            foreach (var t in _diagEntryPool)
                if (t != null) Destroy(t.gameObject);
            _diagEntryPool.Clear();

            if (_report == null)
            {
                AddDiagText("<i>No diagnostic report available yet. Load mods to generate diagnostics.</i>",
                    new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            var all = _report.All;

            // Summary header
            var modSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in all)
                if (!string.IsNullOrEmpty(d.ModId))
                    modSet.Add(d.ModId);

            AddDiagText($"<b>{_report.ErrorCount} error(s), {_report.WarningCount} warning(s) across {modSet.Count} mod(s)</b>",
                Color.white);

            if (all.Count == 0)
            {
                AddDiagText("<i>No diagnostics to display.</i>", new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            string search = searchField != null ? searchField.text : "";

            for (int i = 0; i < all.Count; i++)
            {
                var d = all[i];
                if (!ShouldShowDiagnostic(d, search)) continue;

                var color = d.Severity switch
                {
                    DiagnosticSeverity.Error => new Color(1f, 0.35f, 0.35f),
                    DiagnosticSeverity.Warning => new Color(1f, 0.85f, 0.25f),
                    _ => new Color(0.85f, 0.85f, 0.85f)
                };

                string text = $"[{d.Severity}] [{d.Category}] {d.Message}";
                if (!string.IsNullOrEmpty(d.ModId)) text += $"\n  Mod: {d.ModId}";
                if (!string.IsNullOrEmpty(d.FilePath))
                    text += $"\n  File: {d.FilePath}" + (d.LineNumber >= 0 ? $":{d.LineNumber}" : "");
                if (!string.IsNullOrEmpty(d.DefId)) text += $"\n  Def: {d.DefId}";
                if (!string.IsNullOrEmpty(d.FieldPath)) text += $"\n  Field: {d.FieldPath}";
                if (!string.IsNullOrEmpty(d.ExpectedValue)) text += $"\n  Expected: {d.ExpectedValue}";
                if (!string.IsNullOrEmpty(d.ActualValue)) text += $"\n  Actual: {d.ActualValue}";
                if (!string.IsNullOrEmpty(d.Detail)) text += $"\n  Detail: {d.Detail}";

                AddDiagText(text, color);
            }
        }

        void AddDiagText(string text, Color color)
        {
            if (diagnosticsEntryPrefab != null)
            {
                var go = Instantiate(diagnosticsEntryPrefab, diagnosticsContent);
                var tmp = go.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = text;
                    tmp.color = color;
                    _diagEntryPool.Add(tmp);
                }
            }
        }

        static bool ShouldShowDiagnostic(ContentDiagnostic d, string search)
        {
            if (string.IsNullOrEmpty(search)) return true;

            return (d.Message != null && d.Message.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (d.DefId != null && d.DefId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (d.FilePath != null && d.FilePath.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (d.ModId != null && d.ModId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        #endregion

        #region Loading Log Tab

        void OnLoadProgress(string message)
        {
            _loadingEntries.Add($"[{Time.realtimeSinceStartup:F3}] {message}");
            if (_activeTab == 2) RefreshLoadingLogView(autoScroll: true);
        }

        void OnDiagnosticsReady(DiagnosticReport report)
        {
            _report = report;
            if (_activeTab == 1) RefreshDiagnosticsView();
        }

        void RefreshLoadingLogView(bool autoScroll = false)
        {
            if (loadingLogText == null) return;
            loadingLogText.text = _loadingEntries.Count > 0
                ? string.Join("\n", _loadingEntries)
                : "No loading events captured yet.";

            if (autoScroll && loadingLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                loadingLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        #endregion

        #region Toolbar Actions

        void UpdateBadgeCounts()
        {
            var buffer = logCapture.Buffer;
            if (errorCountText != null) errorCountText.text = buffer.ErrorCount.ToString();
            if (warningCountText != null) warningCountText.text = buffer.WarningCount.ToString();
            if (infoCountText != null) infoCountText.text = buffer.LogCount.ToString();
        }

        void OnClearClicked()
        {
            logCapture.Buffer.Clear();
            _expandedIndex = -1;
            _hasAutoOpened = false;
            _cachedVersion = -1;
        }

        void OnCopyClicked()
        {
            if (_activeTab == 1 && _report != null)
                GUIUtility.systemCopyBuffer = _report.ToFormattedText();
            else
                GUIUtility.systemCopyBuffer = logCapture.Buffer.CopyAll(true);
        }

        void OnPauseClicked()
        {
            logCapture.Paused = !logCapture.Paused;
            if (pauseButtonText != null)
                pauseButtonText.text = logCapture.Paused ? "Resume" : "Pause";
        }

        #endregion
    }
}
