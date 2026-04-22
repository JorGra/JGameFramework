using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UI.Tabs
{
    [Serializable]
    public sealed class UITab
    {
        [Tooltip("Optional identifier used purely for debugging/logging.")]
        public string id;

        [Tooltip("All objects that should be enabled while this tab is active.")]
        public GameObject[] contentRoots = Array.Empty<GameObject>();

        [Tooltip("First selectable that should receive focus when the tab opens.")]
        public GameObject firstSelected;

        public void SetActive(bool isActive)
        {
            if (contentRoots == null) return;

            for (int i = 0; i < contentRoots.Length; i++)
            {
                var root = contentRoots[i];
                if (root == null || root.activeSelf == isActive) continue;
                root.SetActive(isActive);
            }
        }

        public GameObject ResolveFirstSelection()
        {
            if (firstSelected != null) return firstSelected;
            if (contentRoots == null) return null;

            foreach (var root in contentRoots)
            {
                if (root == null) continue;
                var selectable = root.GetComponentInChildren<Selectable>(true);
                if (selectable != null) return selectable.gameObject;
            }
            return null;
        }
    }

    /// <summary>
    /// Drop-in, reusable tab controller. Handles per-player input (local co-op),
    /// optional wrapping, focus, and raises events.
    /// </summary>
    public class UITabNavigator : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private UITab[] tabs = Array.Empty<UITab>();
        [SerializeField] private int defaultTabIndex = 0;
        [SerializeField] private bool wrapTabs = true;

        [Header("Input (per Player)")]
        [SerializeField] private PlayerInput playerInput; // optional, auto-find if null
        [SerializeField] private MultiplayerEventSystem multiplayerEventSystem; // optional, auto-find if null
        [SerializeField] private string nextTabActionName = "NextTab";
        [SerializeField] private string previousTabActionName = "PreviousTab";

        [Header("Events")]
        public UnityEvent<int> onTabChanged;          // arg: new index
        public UnityEvent<int> onTabReselected;       // arg: index (when already active)
        public UnityEvent<GameObject> onTabFocused;   // arg: selection target (may be null)

        public event Action<int> TabChanged;
        public event Action<int> TabReselected;
        public event Action<GameObject> TabFocused;

        private int _activeTabIndex = -1;
        private InputAction _nextAction;
        private InputAction _prevAction;

        public int ActiveTabIndex => _activeTabIndex;
        public int TabCount => tabs?.Length ?? 0;

        #region Lifecycle
        private void Awake()
        {
            InitializeTabs();
        }

        private void OnEnable()
        {
            BindInput();
            UpdateTabVisibility();
        }

        private void OnDisable()
        {
            UnbindInput();
        }

        private void OnDestroy()
        {
            UnbindInput();
        }
        #endregion

        #region Public API
        public void SetTabs(UITab[] newTabs, int startIndex = 0, bool focus = true)
        {
            tabs = newTabs ?? Array.Empty<UITab>();
            defaultTabIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, tabs.Length - 1));
            InitializeTabs();
            UpdateTabVisibility(true);
            if (focus) TryFocusCurrentTab();
        }

        public UITab GetTab(int index)
        {
            if (tabs == null || index < 0 || index >= tabs.Length) return null;
            return tabs[index];
        }

        public int FindTabIndexById(string tabId)
        {
            if (string.IsNullOrWhiteSpace(tabId) || tabs == null) return -1;

            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] != null && string.Equals(tabs[i].id, tabId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public void SelectTab(int index, bool focus = true)
        {
            if (tabs == null || tabs.Length == 0) return;

            index = Mathf.Clamp(index, 0, tabs.Length - 1);
            if (_activeTabIndex == index)
            {
                // Re-select current tab
                onTabReselected?.Invoke(index);
                TabReselected?.Invoke(index);
                UpdateTabVisibility();
                if (focus) TryFocusCurrentTab();
                return;
            }

            _activeTabIndex = index;
            UpdateTabVisibility(true);
            if (focus) TryFocusCurrentTab();
            onTabChanged?.Invoke(_activeTabIndex);
            TabChanged?.Invoke(_activeTabIndex);
        }

        public void NextTab() => Advance(+1);
        public void PrevTab() => Advance(-1);

        public void ConfigurePlayerInput(PlayerInput input, MultiplayerEventSystem mes = null)
        {
            playerInput = input;
            multiplayerEventSystem = mes != null ? mes : multiplayerEventSystem;
            if (multiplayerEventSystem == null) EnsureMultiplayerEventSystem();
            RebindInput();
        }
        #endregion

        #region Internals
        private void InitializeTabs()
        {
            if (tabs == null || tabs.Length == 0)
            {
                _activeTabIndex = -1;
                return;
            }

            defaultTabIndex = Mathf.Clamp(defaultTabIndex, 0, tabs.Length - 1);
            _activeTabIndex = defaultTabIndex;
        }

        private void UpdateTabVisibility(bool forceActive = false)
        {
            if (tabs == null) return;

            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null) continue;

                bool panelActive = forceActive || isActiveAndEnabled || gameObject.activeInHierarchy;
                bool shouldBeActive = panelActive && i == _activeTabIndex;
                tabs[i].SetActive(shouldBeActive);
            }
        }

        private void TryFocusCurrentTab()
        {
            if (this == null) return;
            if (!isActiveAndEnabled || tabs == null || tabs.Length == 0) return;

            EnsureMultiplayerEventSystem();
            if (multiplayerEventSystem == null || !multiplayerEventSystem.enabled) return;

            if (_activeTabIndex < 0 || _activeTabIndex >= tabs.Length) return;

            var tab = tabs[_activeTabIndex];
            if (tab == null) return;

            var selectionTarget = tab.ResolveFirstSelection();
            if (selectionTarget == null || !selectionTarget.activeInHierarchy) return;

            multiplayerEventSystem.SetSelectedGameObject(null);
            multiplayerEventSystem.SetSelectedGameObject(selectionTarget);

            onTabFocused?.Invoke(selectionTarget);
            TabFocused?.Invoke(selectionTarget);
        }

        private void Advance(int direction)
        {
            if (tabs == null || tabs.Length == 0) return;

            if (_activeTabIndex < 0)
                _activeTabIndex = Mathf.Clamp(defaultTabIndex, 0, tabs.Length - 1);

            int target = _activeTabIndex + direction;

            if (wrapTabs)
            {
                if (target < 0) target = tabs.Length - 1;
                else if (target >= tabs.Length) target = 0;
            }
            else
            {
                target = Mathf.Clamp(target, 0, tabs.Length - 1);
            }

            if (target != _activeTabIndex)
            {
                SelectTab(target, true);
            }
            else
            {
                // If we tried to move but stayed (e.g., no wrap and at edge), reselect event can be useful
                onTabReselected?.Invoke(_activeTabIndex);
                TabReselected?.Invoke(_activeTabIndex);
            }
        }
        #endregion

        #region Input
        private void EnsureMultiplayerEventSystem()
        {
            if (multiplayerEventSystem != null) return;
            multiplayerEventSystem = GetComponentInChildren<MultiplayerEventSystem>(true);
            if (multiplayerEventSystem == null)
                multiplayerEventSystem = FindObjectOfType<MultiplayerEventSystem>(true); // safe fallback
        }

        private void BindInput()
        {
            if (tabs == null || tabs.Length == 0) return;

            if (playerInput == null)
                playerInput = GetComponentInParent<PlayerInput>();

            if (playerInput == null) return;

            UnbindInput();

            if (!string.IsNullOrWhiteSpace(nextTabActionName))
            {
                _nextAction = playerInput.actions?.FindAction(nextTabActionName, false);
                if (_nextAction != null)
                {
                    _nextAction.performed += OnNextPerformed;
                    if (!_nextAction.enabled) _nextAction.Enable();
                }
            }

            if (!string.IsNullOrWhiteSpace(previousTabActionName))
            {
                _prevAction = playerInput.actions?.FindAction(previousTabActionName, false);
                if (_prevAction != null)
                {
                    _prevAction.performed += OnPrevPerformed;
                    if (!_prevAction.enabled) _prevAction.Enable();
                }
            }
        }

        private void RebindInput()
        {
            UnbindInput();
            BindInput();
        }

        private void UnbindInput()
        {
            if (_nextAction != null)
            {
                _nextAction.performed -= OnNextPerformed;
                if (_nextAction.enabled) _nextAction.Disable();
            }

            if (_prevAction != null)
            {
                _prevAction.performed -= OnPrevPerformed;
                if (_prevAction.enabled) _prevAction.Disable();
            }

            _nextAction = null;
            _prevAction = null;
        }

        private void OnNextPerformed(InputAction.CallbackContext ctx)
        {
            if (this == null) return;
            if (!ctx.performed) return;
            NextTab();
        }

        private void OnPrevPerformed(InputAction.CallbackContext ctx)
        {
            if (this == null) return;
            if (!ctx.performed) return;
            PrevTab();
        }
        #endregion
    }
}
