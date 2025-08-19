using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AccordionRow : UIBehaviour, IPointerEnterHandler, ILayoutElement
{
    [Header("Refs")]
    public RectTransform header;     // required
    public RectTransform content;    // required; clipped by RectMask2D

    [Header("Cases ScrollRect")]
    public ScrollRect casesScrollRect;       // assign in inspector
    public RectTransform casesViewport;      // usually SR.viewport
    public RectTransform casesContent;       // usually SR.content (CaseStrip)

    [SerializeField] float headerContentSpacing = 0f;
    [SerializeField] RectOffset innerPadding;         // viewport inner margins
    [HideInInspector] public AccordionListController manager;

    [SerializeField] bool hideScrollbarWhenCollapsed = true;
    [SerializeField] float collapsedThreshold = 1f; // px; <= this = collapsed

    float _contentPrefHeight = -1f;

    float _headerHeight = -1f;
    float _animContentHeight = 0f;
    float _velocity;

    Scrollbar _cachedHScrollbar;

    // --- ILayoutElement ---
    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical() { } // values provided via properties below
    public float minWidth => -1;
    public float preferredWidth => -1;
    public float flexibleWidth => 1;
    public float minHeight => HeaderHeight;
    public float preferredHeight => HeaderHeight + _animContentHeight;
    public float flexibleHeight => -1;
    public int layoutPriority => 1;

    float HeaderHeight
    {
        get
        {
            if (_headerHeight < 0f && header)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(header);
                _headerHeight = Mathf.Ceil(LayoutUtility.GetPreferredHeight(header));
            }
            return Mathf.Max(0f, _headerHeight);
        }
    }

    public float CollapsedHeaderHeight => HeaderHeight;
    protected override void OnEnable()
    {
        base.OnEnable();

        if (casesScrollRect)
        {
            casesScrollRect.horizontal = true;
            casesScrollRect.vertical = false;

            // keep ScrollRect from auto-toggling
            casesScrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            // cache BEFORE SnapContent might detach it
            if (_cachedHScrollbar == null)
                _cachedHScrollbar = casesScrollRect.horizontalScrollbar;
        }

        SnapContent(0f); // collapse after caching
    }

    public void OnPointerEnter(PointerEventData eventData) => manager?.RequestOpen(this);

    public void AnimateContent(float targetContent, float smoothTime)
    {
        _animContentHeight = Mathf.SmoothDamp(_animContentHeight, targetContent, ref _velocity, smoothTime);
        ApplyContentSize();
        ApplyScrollbarVisibility();
    }

    public void SnapContent(float targetContent)
    {
        _velocity = 0f;
        _animContentHeight = Mathf.Max(0f, targetContent);
        ApplyContentSize();
        ApplyScrollbarVisibility();
    }

    void ApplyContentSize()
    {
        if (!content) return;

        // Top-anchor content container and place it just below the header
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, HeaderHeight + headerContentSpacing, _animContentHeight);

        // Make the ScrollRect fill the content container with adjustable inner padding
        if (casesScrollRect)
        {
            var srRT = (RectTransform)casesScrollRect.transform;
            srRT.anchorMin = Vector2.zero;
            srRT.anchorMax = Vector2.one;
            srRT.pivot = new Vector2(0.5f, 0.5f);
            srRT.offsetMin = new Vector2(innerPadding.left, innerPadding.bottom);
            srRT.offsetMax = new Vector2(-innerPadding.right, -innerPadding.top);
        }

        // Keep header visually on top
        header?.SetAsLastSibling();

        // Let parent layout reflow
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
    }

    // Preferred height of the cases strip (tallest CaseItem + paddings)
    public float GetContentPreferredHeight()
    {
        var t = casesContent ? casesContent : content;
        if (_contentPrefHeight < 0f)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(t);
            _contentPrefHeight = Mathf.Ceil(LayoutUtility.GetPreferredHeight(t));
        }
        return _contentPrefHeight;
    }

    public void InvalidateContentLayout()
    {
        _contentPrefHeight = -1f;
        Canvas.ForceUpdateCanvases();
    }

    public void ResetCasesScroll()
    {
        if (casesScrollRect) casesScrollRect.horizontalNormalizedPosition = 0f;
    }

    public void InvalidateHeader() => _headerHeight = -1f;

    void ApplyScrollbarVisibility()
    {
        if (!hideScrollbarWhenCollapsed || casesScrollRect == null) return;

        bool shouldShow = _animContentHeight > collapsedThreshold;

        if (shouldShow)
        {
            // Re-attach if needed and show
            if (casesScrollRect.horizontalScrollbar == null && _cachedHScrollbar != null)
                casesScrollRect.horizontalScrollbar = _cachedHScrollbar;

            if (_cachedHScrollbar != null && !_cachedHScrollbar.gameObject.activeSelf)
                _cachedHScrollbar.gameObject.SetActive(true);

            // allow horizontal gestures again
            casesScrollRect.horizontal = true;
        }
        else
        {
            // Detach first so ScrollRect won't re-enable it later
            if (casesScrollRect.horizontalScrollbar != null)
                casesScrollRect.horizontalScrollbar = null;

            if (_cachedHScrollbar != null && _cachedHScrollbar.gameObject.activeSelf)
                _cachedHScrollbar.gameObject.SetActive(false);

            // also disable horizontal gestures while collapsed
            casesScrollRect.horizontal = false;
        }
    }
}