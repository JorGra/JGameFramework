using System.Collections.Generic;
using JGameFramework.UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public sealed class TooltipTester : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

    private static TooltipTester s_activeOwner;
    private static TooltipHandle s_activeHandle;
    private static bool s_hasActiveHandle;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void OnDisable()
    {
        if (IsActiveOwner)
        {
            DismissActiveTooltip();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsActiveOwner)
        {
            return;
        }

        if (tooltipType == TooltipType.Actions)
        {
            return;
        }

        DismissActiveTooltip();
    }

    private void ShowTooltip(PointerEventData eventData)
    {
        CloseExistingHandle();

        var builder = new TooltipRequestBuilder()
            .WithAnchor(_rectTransform, follow: true)
            .WithPlayerContext(ResolvePlayerContext(eventData))
            .WithSortingOffset(tooltipType == TooltipType.Actions ? 50 : 0)
            .WithTag(this);

        switch (tooltipType)
        {
            case TooltipType.SimpleText:
                ConfigureSimpleText(builder);
                break;
            case TooltipType.Image:
                ConfigureImage(builder);
                break;
            case TooltipType.Actions:
                ConfigureActions(builder);
                break;
        }

        var handle = builder.Show();
        if (!handle.IsValid)
        {
            return;
        }

        s_activeOwner = this;
        s_activeHandle = handle;
        s_hasActiveHandle = true;
    }

    private void ConfigureSimpleText(TooltipRequestBuilder builder)
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

    private void ConfigureImage(TooltipRequestBuilder builder)
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

    private void ConfigureActions(TooltipRequestBuilder builder)
    {
        builder
            .AsStickyTooltip()
            .WithBlocksRaycasts(true)
            .WithPivot(new Vector2(0.5f, 0f))
            .WithOffset(new Vector2(0f, 18f))
            .WithClampOverride(false);

        builder.AddContent(new TooltipTextBlockData
        {
            Header = "Context Actions",
            Body = "Click the buttons below. The first two keep the tooltip open, the last one closes it.",
            ShowHeader = true
        });
        builder.AddContent(new TooltipSpacerData { Height = 6f });
        builder.AddContent(new TooltipKeyValueRowData { Label = "Blocks Raycasts", Value = "Yes" });

        var actions = new List<TooltipActionData>();

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
                        Body = "Use the other actions to keep the tooltip open or close it manually.",
                        ShowHeader = true
                    }
                };

                handle.ReplaceContent(refreshedContent, actions);
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
            label: "Close Tooltip",
            callback: (handle, context) =>
            {
                Debug.Log("Closing tooltip");
            });

        actions.Add(inspectAction);
        actions.Add(pinAction);
        actions.Add(closeAction);

        foreach (var action in actions)
        {
            builder.AddAction(action);
        }
    }

    private void DismissActiveTooltip()
    {
        if (!s_hasActiveHandle)
        {
            return;
        }

        if (s_activeHandle.IsValid)
        {
            s_activeHandle.Close();
        }

        ClearActiveHandle();
    }

    private static void CloseExistingHandle()
    {
        if (s_hasActiveHandle && s_activeHandle.IsValid)
        {
            s_activeHandle.Close();
        }

        ClearActiveHandle();
    }

    private static void ClearActiveHandle()
    {
        s_activeOwner = null;
        s_activeHandle = default;
        s_hasActiveHandle = false;
    }

    private bool IsActiveOwner => s_hasActiveHandle && ReferenceEquals(s_activeOwner, this) && s_activeHandle.IsValid;

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
