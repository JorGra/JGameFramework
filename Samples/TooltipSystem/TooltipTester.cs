using System.Collections.Generic;
using JGameFramework.UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public sealed class TooltipTester : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum TooltipType
    {
        SimpleText,
        Image,
        Actions
    }

    [SerializeField] private TooltipType tooltipType = TooltipType.SimpleText;
    [SerializeField] private Sprite imageSprite;
    [SerializeField, TextArea] private string simpleHeader = "Sample Header";
    [SerializeField, TextArea] private string simpleBody = "Hover each corner button to preview the tooltip system.";

    private RectTransform _rectTransform;

    private static TooltipTester s_hoverOwner;
    private static TooltipHandle s_hoverHandle;
    private static bool s_hasHoverHandle;

    private static TooltipTester s_menuOwner;
    private static TooltipHandle s_menuHandle;
    private static bool s_hasMenuHandle;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnDisable()
    {
        if (IsHoverOwner)
        {
            DismissHoverTooltip();
        }

        if (IsMenuOwner)
        {
            DismissContextMenu();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipType == TooltipType.Actions)
        {
            return;
        }

        ShowHoverTooltip(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipType == TooltipType.Actions)
        {
            return;
        }

        if (IsHoverOwner)
        {
            DismissHoverTooltip();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (tooltipType != TooltipType.Actions)
        {
            return;
        }

        if (IsMenuOwner)
        {
            DismissContextMenu();
        }
        else
        {
            ShowContextMenu(eventData);
        }
    }

    private void ShowHoverTooltip(PointerEventData eventData)
    {
        CloseHoverTooltip();

        var context = ResolvePlayerContext(eventData);
        var builder = new TooltipBuilder()
            .WithAnchor(_rectTransform, follow: true)
            .WithPlayerContext(context)
            .WithTag(this);

        switch (tooltipType)
        {
            case TooltipType.SimpleText:
                ConfigureSimpleTooltip(builder);
                break;
            case TooltipType.Image:
                ConfigureImageTooltip(builder);
                break;
        }

        var handle = builder.Show();
        if (!handle.IsValid)
        {
            return;
        }

        s_hoverOwner = this;
        s_hoverHandle = handle;
        s_hasHoverHandle = true;

        if (IsMenuOwner)
        {
            DismissContextMenu();
        }
    }

    private void ShowContextMenu(PointerEventData eventData)
    {
        CloseContextMenu();

        var context = ResolvePlayerContext(eventData);
        var builder = new ContextMenuBuilder()
            .WithAnchor(_rectTransform, follow: false)
            .WithOffset(new Vector2(0f, 18f))
            .WithPivot(new Vector2(0.5f, 0f))
            .WithClampOverride(false)
            .WithPlayerContext(context)
            .WithTag(this)
            .WithSortingOffset(50)
            .WithBlocksRaycasts(true)
            .AsStickyTooltip(true);

        ConfigureContextMenu(builder);

        var handle = builder.Show();
        if (!handle.IsValid)
        {
            return;
        }

        s_menuOwner = this;
        s_menuHandle = handle;
        s_hasMenuHandle = true;

        if (IsHoverOwner)
        {
            DismissHoverTooltip();
        }
    }

    private void ConfigureSimpleTooltip(TooltipBuilder builder)
    {
        builder
            .WithPivot(new Vector2(0f, 1f))
            .WithOffset(new Vector2(24f, -16f))
            .WithClampOverride(true);

        builder.AddContent(new TooltipTextBlockData
        {
            Header = string.IsNullOrWhiteSpace(simpleHeader) ? "Simple Tooltip" : simpleHeader,
            Body = string.IsNullOrWhiteSpace(simpleBody) ? "Demonstrates text blocks, layout spacing, and follow behaviour." : simpleBody,
            ShowHeader = true
        });

        builder.AddContent(new TooltipSpacerData { Height = 8f });
        builder.AddContent(new TooltipKeyValueRowData { Label = "Follows target", Value = "Yes" });
    }

    private void ConfigureImageTooltip(TooltipBuilder builder)
    {
        builder
            .WithPivot(new Vector2(1f, 1f))
            .WithOffset(new Vector2(-32f, -12f))
            .WithClampOverride(true);

        builder.AddContent(new TooltipImageBlockData
        {
            Sprite = imageSprite,
            PreferredSize = new Vector2(128f, 128f),
            PreserveAspect = true,
            Caption = imageSprite != null ? imageSprite.name : "Assign a sprite to preview",
            CaptionSpacing = 6f
        });

        builder.AddContent(new TooltipSpacerData { Height = 6f });
        builder.AddContent(new TooltipTextBlockData
        {
            Header = "Image Preview",
            Body = "This tooltip mixes image and text blocks and keeps the layout clamped to screen edges.",
            ShowHeader = true
        });
    }

    private void ConfigureContextMenu(ContextMenuBuilder builder)
    {
        builder.AddContent(new TooltipTextBlockData
        {
            Header = "Context Actions",
            Body = "Click the buttons below. The first two keep the menu open, the last one closes it.",
            ShowHeader = true
        });
        builder.AddContent(new TooltipSpacerData { Height = 6f });
        builder.AddContent(new TooltipKeyValueRowData { Label = "Blocks Raycasts", Value = "Yes" });

        var menuActions = new List<TooltipActionData>();

        var inspectAction = new TooltipActionData(
            label: "Inspect",
            callback: (handle, context) =>
            {
                Debug.Log("Inspect action executed");

                var refreshedContent = new List<TooltipContentData>
                {
                    new TooltipTextBlockData
                    {
                        Header = "Inspect",
                        Body = $"Triggered at {Time.realtimeSinceStartup:F2}s",
                        ShowHeader = true
                    },
                    new TooltipSpacerData { Height = 4f },
                    new TooltipTextBlockData
                    {
                        Header = "Tip",
                        Body = "Use the other actions to keep the menu open or close it manually.",
                        ShowHeader = true
                    }
                };

                handle.ReplaceContent(refreshedContent, menuActions);
            },
            closeOnTrigger: false);

        var pinAction = new TooltipActionData(
            label: "Toggle Pin",
            callback: (handle, context) =>
            {
                Debug.Log("Pin action executed");
            },
            closeOnTrigger: false);

        var closeAction = new TooltipActionData(
            label: "Close Menu",
            callback: (handle, context) =>
            {
                Debug.Log("Closing context menu");
            });

        menuActions.Add(inspectAction);
        menuActions.Add(pinAction);
        menuActions.Add(closeAction);

        builder.AddActions(menuActions);
    }

    private void DismissHoverTooltip()
    {
        if (!s_hasHoverHandle)
        {
            return;
        }

        if (s_hoverHandle.IsValid)
        {
            s_hoverHandle.Close();
        }

        ClearHoverHandle();
    }

    private static void CloseHoverTooltip()
    {
        if (!s_hasHoverHandle)
        {
            return;
        }

        if (s_hoverHandle.IsValid)
        {
            s_hoverHandle.Close();
        }

        ClearHoverHandle();
    }

    private void DismissContextMenu()
    {
        if (!s_hasMenuHandle)
        {
            return;
        }

        if (s_menuHandle.IsValid)
        {
            s_menuHandle.Close();
        }

        ClearMenuHandle();
    }

    private static void CloseContextMenu()
    {
        if (!s_hasMenuHandle)
        {
            return;
        }

        if (s_menuHandle.IsValid)
        {
            s_menuHandle.Close();
        }

        ClearMenuHandle();
    }

    private static void ClearHoverHandle()
    {
        s_hoverOwner = null;
        s_hoverHandle = default;
        s_hasHoverHandle = false;
    }

    private static void ClearMenuHandle()
    {
        s_menuOwner = null;
        s_menuHandle = default;
        s_hasMenuHandle = false;
    }

    private bool IsHoverOwner
    {
        get
        {
            if (!s_hasHoverHandle)
            {
                return false;
            }

            if (!IsHandleAlive(s_hoverHandle))
            {
                ClearHoverHandle();
                return false;
            }

            return ReferenceEquals(s_hoverOwner, this);
        }
    }

    private bool IsMenuOwner
    {
        get
        {
            if (!s_hasMenuHandle)
            {
                return false;
            }

            if (!IsHandleAlive(s_menuHandle))
            {
                ClearMenuHandle();
                return false;
            }

            return ReferenceEquals(s_menuOwner, this);
        }
    }

    private static bool IsHandleAlive(TooltipHandle handle)
    {
        if (!handle.IsValid)
        {
            return false;
        }

        if (handle.View == null)
        {
            return false;
        }

        return handle.View.gameObject.activeInHierarchy;
    }

    private static TooltipPlayerContext ResolvePlayerContext(PointerEventData eventData)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return default;
        }

        var uiCamera = eventData?.enterEventCamera ?? eventData?.pressEventCamera ?? Camera.main;
        return TooltipPlayerContextUtility.FromEventSystem(eventSystem, uiCamera);
    }
}


