using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AccordionListController : MonoBehaviour
{
    [Header("Layout Root (has VerticalLayoutGroup)")]
    public RectTransform layoutRoot;        // REQUIRED: the object with the VerticalLayoutGroup
    public VerticalLayoutGroup vlg;         // assign in inspector
    public ScrollRect scrollRect;           // optional; if set, we use viewport height

    [Header("Timing")]
    public float smoothTime = 0.15f;

    [Header("Rows (filled at runtime)")]
    public List<AccordionRow> rows = new();

    AccordionRow _open;

    void Awake()
    {
        if (!vlg) vlg = layoutRoot ? layoutRoot.GetComponent<VerticalLayoutGroup>() : null;
    }

    void Start()
    {
        foreach (var r in rows) r.manager = this;
        CollapseAll();
        ForceRebuildNow();
    }

    public void RequestOpen(AccordionRow row)
    {
        if (row == _open) return;
        _open = row;
        foreach (var r in rows) if (r != _open) r.SnapContent(0f);
        ForceRebuildNow();
    }

    void Update()
    {
        if (_open == null) return;

        float available = GetAvailableHeight();
        float padding = vlg ? vlg.padding.top + vlg.padding.bottom : 0f;
        float spacing = vlg ? vlg.spacing : 0f;

        // Sum other headers
        float sumOtherHeaders = 0f;
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] != _open) sumOtherHeaders += rows[i].CollapsedHeaderHeight;

        float totalSpacing = spacing * Mathf.Max(0, rows.Count - 1);

        // Remaining height the open row COULD take (viewport clamp)
        float remainingForOpenContent =
            available - padding - totalSpacing - sumOtherHeaders - _open.CollapsedHeaderHeight;

        remainingForOpenContent = Mathf.Max(0f, remainingForOpenContent);

        // NEW: measure how much height the content actually WANTS
        float contentPref = _open.GetContentPreferredHeight();

        // Target is the smaller of (what we have) and (what content needs)
        float targetContent = Mathf.Min(remainingForOpenContent, contentPref);

        _open.AnimateContent(targetContent, smoothTime);

        // Reflow siblings
        LayoutRebuilder.MarkLayoutForRebuild(layoutRoot);
    }

    float GetAvailableHeight()
    {
        if (scrollRect && scrollRect.viewport)
            return ((RectTransform)scrollRect.viewport).rect.height; // visible area
        return layoutRoot.rect.height; // non-scroll panel
    }

    void CollapseAll()
    {
        foreach (var r in rows) r.SnapContent(0f);
    }

    void ForceRebuildNow()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        Canvas.ForceUpdateCanvases();
    }
}
