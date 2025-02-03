using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Simple data container for a radial menu item.
/// </summary>
[Serializable]
public class RadialMenuItemData
{
    public string Label;
    public Sprite Icon;
    public UnityEvent OnClick;
    public bool IsPinned;   // True if this should be placed in the pinned angle arc
}

/// <summary>
/// A radial menu that supports design-time assignment of items
/// and runtime addition/removal of items. 
/// </summary>
public class UIRadialMenu : UIStickyPanel
{
    [Header("References")]
    [Tooltip("Parent RectTransform where radial buttons are placed.")]
    public RectTransform menuParent;

    [Tooltip("Prefab used for pinned (fixed) item(s).")]
    public GameObject pinnedButtonPrefab;

    [Tooltip("Prefab used for flexible (non-pinned) item(s).")]
    public GameObject flexibleButtonPrefab;

    [Tooltip("A shared context menu object (with Icon/Label etc.) that appears on first click.")]
    public GameObject sharedContextMenu;

    [Header("Angles")]
    [Tooltip("Starting angle for pinned item(s).")]
    public float pinnedAngle = 255f;
    [Tooltip("Arc width in degrees for pinned item(s).")]
    public float pinnedAngleArc = 90f;

    [Header("Settings")]
    [Tooltip("Radius from the center where the buttons are placed.")]
    public float radius = 200f;
    [Range(0f, 1f), Tooltip("Fraction of the total arc used as spacing between flexible items.")]
    public float marginFraction = 1f;

    [Header("Context Menu Offset")]
    [Tooltip("Distance from the radial button to position the context menu.")]
    public float contextMenuDistance = 50f;
    public float topThreshold = 45f;
    public float leftThreshold = 135f;
    public float bottomThreshold = 225f;
    public float rightThreshold = 315f;

    [Header("Menu Items (Design Time)")]
    [Tooltip("List of items to appear in this menu at design time. You can add to this at runtime too.")]
    public List<RadialMenuItemData> menuItems = new List<RadialMenuItemData>();

    // The "active" button that currently has the context menu open (if any)
    private RadialMenuButton activeButton = null;
    private RectTransform contextMenuRect;

    // Internal collection of created radial buttons
    private List<RadialMenuButton> buttons = new List<RadialMenuButton>();

    protected override void Awake()
    {
        base.Awake();

        // Ensure the context menu is a child of menuParent, if assigned
        if (sharedContextMenu != null)
        {
            contextMenuRect = sharedContextMenu.GetComponent<RectTransform>();
            if (contextMenuRect.parent != menuParent)
            {
                contextMenuRect.SetParent(menuParent, false);
            }
            sharedContextMenu.SetActive(false);
        }
    }

    protected override void Start()
    {
        base.Start();

        // Create radial buttons from the inspector-assigned menuItems
        InitializeMenu();
    }

    protected override void Update()
    {
        base.Update();

        // Reposition the context menu (if open) in case the radial menu moves
        if (activeButton != null && sharedContextMenu != null && sharedContextMenu.activeSelf)
        {
            RepositionContextMenu(activeButton);
        }
    }

    #region Public API

    /// <summary>
    /// Clears existing buttons, then creates new ones
    /// from the current menuItems list.
    /// Call this whenever menuItems is changed or you want
    /// to rebuild from scratch.
    /// </summary>
    public void InitializeMenu()
    {
        // Destroy any existing button objects
        foreach (var btn in buttons)
        {
            if (btn != null && btn.gameObject != null)
                Destroy(btn.gameObject);
        }
        buttons.Clear();

        // Create pinned items and flexible items
        foreach (var data in menuItems)
        {
            if (data.IsPinned)
                CreateButton(data, pinnedButtonPrefab);
            else
                CreateButton(data, flexibleButtonPrefab);
        }

        Redistribute();
    }

    /// <summary>
    /// Adds a new item (pinned or not) at runtime.
    /// You can call Refresh() afterwards to reorder if needed.
    /// </summary>
    public void AddItem(RadialMenuItemData itemData)
    {
        menuItems.Add(itemData);

        // Optionally, create the button immediately...
        if (itemData.IsPinned)
            CreateButton(itemData, pinnedButtonPrefab);
        else
            CreateButton(itemData, flexibleButtonPrefab);

        // Then re-distribute
        Redistribute();
    }

    /// <summary>
    /// Removes an existing item from the list (if present),
    /// destroys the corresponding button, and refreshes layout.
    /// </summary>
    public void RemoveItem(RadialMenuItemData itemData)
    {
        // Find the button that matches itemData
        RadialMenuButton foundButton = buttons.Find(b => b.ItemData == itemData);
        if (foundButton != null)
        {
            buttons.Remove(foundButton);
            if (foundButton.gameObject != null)
                Destroy(foundButton.gameObject);
        }

        // Remove from data list if present
        if (menuItems.Contains(itemData))
        {
            menuItems.Remove(itemData);
        }

        Redistribute();
    }

    /// <summary>
    /// Re-applies angle distribution for all buttons,
    /// without destroying and recreating them.
    /// Call this after you programmatically add or remove multiple items.
    /// </summary>
    public void Refresh()
    {
        Redistribute();
    }

    public override void SetTarget(Transform newTarget)
    {
        base.SetTarget(newTarget);
        HideContextMenu();
    }
    #endregion

    #region Creation & Distribution

    private void CreateButton(RadialMenuItemData data, GameObject prefab)
    {
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, menuParent);
        var btn = go.GetComponent<RadialMenuButton>();

        // Store the item data on the button for reference
        btn.ItemData = data;

        // Set label/icon
        btn.SetLabel(data.Label);
        btn.SetIcon(data.Icon);

        // Hook up onClick event
        if (data.OnClick != null)
        {
            btn.onClick.AddListener(() => data.OnClick.Invoke());
        }
        else
        {
            // Default debug
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"{data.Label} clicked!");
            });
        }

        // Also let the button know which radial menu it belongs to
        btn.hostMenu = this;

        // Initially place at (0,0). We'll do the final distribution in Redistribute().
        var r = go.GetComponent<RectTransform>();
        r.anchoredPosition = Vector2.zero;
        r.localRotation = Quaternion.identity;

        buttons.Add(btn);
    }

    private void Redistribute()
    {
        if (buttons.Count == 0) return;

        // Separate pinned from flexible
        List<RadialMenuButton> pinnedList = new List<RadialMenuButton>();
        List<RadialMenuButton> flexibleList = new List<RadialMenuButton>();

        foreach (var btn in buttons)
        {
            if (btn.ItemData != null && btn.ItemData.IsPinned)
                pinnedList.Add(btn);
            else
                flexibleList.Add(btn);
        }

        // Place pinned items within the pinnedAngleArc.
        // If you want exactly one pinned, you can skip subdividing pinnedAngleArc.
        float pinnedStep = (pinnedList.Count > 1)
            ? pinnedAngleArc / pinnedList.Count
            : pinnedAngleArc;

        for (int i = 0; i < pinnedList.Count; i++)
        {
            float angle = pinnedAngle + (pinnedStep * i) + (pinnedStep / 2f);
            pinnedList[i].SetAngle(angle);

            var rect = pinnedList[i].GetComponent<RectTransform>();
            rect.anchoredPosition = AngleToPosition(angle, radius);
            rect.localRotation = Quaternion.identity;
        }

        // Flexible items occupy the leftover arc: 360 - pinnedAngleArc
        float leftoverArc = 360f - pinnedAngleArc;
        int flexibleCount = flexibleList.Count;
        if (flexibleCount > 0)
        {
            float totalMargin = leftoverArc * marginFraction;
            float gap = totalMargin / flexibleCount;
            float arcForItems = leftoverArc - totalMargin;
            float slice = arcForItems / flexibleCount;
            float startAngle = pinnedAngle + pinnedAngleArc;

            for (int i = 0; i < flexibleCount; i++)
            {
                float angleDeg = startAngle + gap * 0.5f + i * (slice + gap);
                flexibleList[i].SetAngle(angleDeg);

                var r = flexibleList[i].GetComponent<RectTransform>();
                r.anchoredPosition = AngleToPosition(angleDeg, radius);
                r.localRotation = Quaternion.identity;
            }
        }
    }

    private Vector2 AngleToPosition(float angleDeg, float dist)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;
    }

    #endregion

    #region Click Logic

    /// <summary>
    /// Called by a RadialMenuButton when it is clicked. We handle
    /// "first click vs second click" logic to show/hide context menu.
    /// </summary>
    public void OnButtonClicked(RadialMenuButton clickedButton)
    {
        // Case A: No button is currently active => open menu for clickedButton
        if (activeButton == null)
        {
            // first click
            activeButton = clickedButton;
            ShowContextMenu(clickedButton);
            return;
        }

        // Case B: The user clicked the *same* button that is already active
        if (activeButton == clickedButton)
        {
            // second click => invoke button action, then close the menu
            clickedButton.onClick.Invoke();
            HideContextMenu();  // sets activeButton = null
        }
        else
        {
            // Case C: The user clicked a *different* button 
            // => instantly switch to that new button's menu (this is a "first click" on that button).
            HideContextMenu();
            activeButton = clickedButton;
            ShowContextMenu(clickedButton);
        }
    }

    private void ShowContextMenu(RadialMenuButton button)
    {
        if (!sharedContextMenu) return;

        RepositionContextMenu(button);
        UpdateContextMenuContent(button);
        sharedContextMenu.SetActive(true);
    }

    public void HideContextMenu()
    {
        if (sharedContextMenu && sharedContextMenu.activeSelf)
        {
            sharedContextMenu.SetActive(false);
        }
        activeButton = null;
    }

    #endregion

    #region Context Menu Positioning

    /// <summary>
    /// Positions the context menu around the active button by adjusting pivot & offset.
    /// (Rectangular alignment logic.)
    /// </summary>
    private void RepositionContextMenu(RadialMenuButton button)
    {
        if (!sharedContextMenu) return;

        float normalizedAngle = button.angle % 360f;
        if (normalizedAngle < 0) normalizedAngle += 360f;

        Vector2 pivot = new Vector2(0.5f, 0.5f);
        Vector2 localOffset = Vector2.zero;

        if (normalizedAngle >= topThreshold && normalizedAngle < leftThreshold)
        {
            // Above => pivot is bottom center
            pivot = new Vector2(0.5f, 0f);
            localOffset = new Vector2(0f, contextMenuDistance);
        }
        else if (normalizedAngle >= leftThreshold && normalizedAngle < bottomThreshold)
        {
            // Left => pivot is right center
            pivot = new Vector2(1f, 0.5f);
            localOffset = new Vector2(-contextMenuDistance, 0f);
        }
        else if (normalizedAngle >= bottomThreshold && normalizedAngle < rightThreshold)
        {
            // Below => pivot is top center
            pivot = new Vector2(0.5f, 1f);
            localOffset = new Vector2(0f, -contextMenuDistance);
        }
        else
        {
            // Right => pivot is left center
            pivot = new Vector2(0f, 0.5f);
            localOffset = new Vector2(contextMenuDistance, 0f);
        }

        contextMenuRect.pivot = pivot;

        RectTransform btnRect = button.GetComponent<RectTransform>();
        contextMenuRect.anchoredPosition = btnRect.anchoredPosition + localOffset;
    }

    private void UpdateContextMenuContent(RadialMenuButton button)
    {
        if (!sharedContextMenu) return;

        Image iconImage = sharedContextMenu.transform.Find("Icon")?.GetComponent<Image>();
        Text labelText = sharedContextMenu.transform.Find("Label")?.GetComponent<Text>();

        if (iconImage != null) iconImage.sprite = button.Icon;
        if (labelText != null) labelText.text = button.LabelText;
    }

    #endregion
}
