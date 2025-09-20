# Tooltip System Setup & Usage Guide

This document explains how to configure and extend the tooltip framework located in `Assets/JGameFramework/Scripts/UISystem/TooltipSystem`. Follow the steps in order to get the system running in any UGUI/TMPro based Unity project.

## 1. Required Assets

1. **Canvas** – A persistent UGUI canvas (Screen Space Overlay or Screen Space Camera).
2. **Tooltip layer** – An empty `RectTransform` that will host all tooltip instances (usually a full-screen child of the canvas).
3. **Tooltip view prefab** – A prefab with the visual layout for a tooltip (`TooltipView` component).
4. **Action button prefab** – A prefab for context actions (`TooltipActionButtonView` component).
5. **Content prefabs** – One prefab per tooltip content type (e.g., text block, image block). Each prefab must implement `TooltipContentViewBase`.

## 2. Canvas & Layer Setup

1. Create or select a UGUI canvas. If you use *Screen Space – Camera*, assign the camera in the canvas component.
2. Add a child `RectTransform` named e.g. **TooltipLayer**. Stretch it to match the canvas (anchor min/max at (0,0) and (1,1), zero offsets).
3. Ensure the layer has no layout components; it only acts as a container for spawned tooltips.

## 3. Tooltip View Prefab

1. Create an empty prefab (e.g., **Tooltip_Default**).
2. Add components:
   - `RectTransform`
   - Background graphics (e.g., `Image`, `Shadow`, `Outline`).
   - `VerticalLayoutGroup` and `ContentSizeFitter` (Preferred Size) to grow dynamically.
   - `CanvasGroup` (optional but recommended).
   - `TooltipView` component.
3. In the inspector, assign the serialized fields on `TooltipView`:
   - **Root**: the prefab root `RectTransform`.
   - **Content Root**: the transform where content blocks will be placed (often same as root or a nested layout group).
   - **Actions Root** (optional): a horizontal or vertical layout used for action buttons.
   - **Actions Layout** (optional): layout component managing the action button row.
   - **CanvasGroup / Canvas** references if the prefab contains them.
4. Save the prefab and drag it into `TooltipSystemRoot._tooltipPrefab`.

### Optional: Multiple Visual Styles
Create alternative tooltip view prefabs (e.g., for loot, quests) and expose them via project-specific managers. `TooltipSystemRoot` only needs one default prefab but you can instantiate alternate prefabs manually if desired.

## 4. Action Button Prefab

1. Create a UGUI `Button` prefab.
2. Add a `TextMeshProUGUI` for the label and (optionally) an `Image` for the icon.
3. Replace the default `Button` script with `TooltipActionButtonView` (it inherits from `Button`).
4. Assign serialized fields (`_label`, `_icon`).
5. Reference this prefab in `TooltipSystemRoot._actionButtonPrefab`.

## 5. Content Prefabs

The framework ships with four ready-to-use view scripts:

| Script | Expected Content | Notes |
| --- | --- | --- |
| `TooltipTextBlockView` | `TooltipTextBlockData` | Header + body text, configurable fonts/colors. |
| `TooltipImageBlockView` | `TooltipImageBlockData` | Sprite display with optional caption. |
| `TooltipKeyValueRowView` | `TooltipKeyValueRowData` | Label/value text pair. |
| `TooltipSpacerView` | `TooltipSpacerData` | Vertical spacer for layout control. |

**To Build a Content Prefab**

1. Create an empty prefab.
2. Add required child objects (TMP texts, Images, LayoutGroups…).
3. Add the matching `TooltipContentView` component.
4. Drag the prefab into the `TooltipSystemRoot._contentPrefabs` list.

`TooltipSystemRoot` maps each prefab to the `TooltipContentData` type exposed by `SupportedDataType`. At runtime you can register additional mappings through `TooltipSystemRoot.RegisterContentPrefab`.

## 6. Installing the TooltipSystemRoot

1. Create an empty GameObject inside the canvas (e.g., **TooltipSystem**).
2. Add the `TooltipSystemRoot` component.
3. Assign fields:
   - **Canvas**: the hosting canvas.
   - **Tooltip Layer**: the full-screen container prepared earlier.
   - **Tooltip Prefab**: your tooltip view prefab.
   - **Action Button Prefab**: the button prefab.
   - **Content Prefabs**: drag every content prefab into the list.
4. Optional settings:
   - **Clamp To Viewport** (default `true`): keeps tooltips inside screen bounds.
   - **Default Screen Offset**: base offset applied to every tooltip.

The root acts as a singleton accessed on demand through `TooltipSystemRoot.Instance`.

## 7. Showing Tooltips

### Hover Tooltips (`TooltipBuilder`)
Use this builder for passive information popups that appear on hover or focus and should never block interaction with the world beneath them.

```csharp
var tooltipHandle = new TooltipBuilder()
    .WithAnchor(slotRect, follow: true)
    .WithPlayerContext(TooltipPlayerContextUtility.FromEventSystem(EventSystem.current, uiCamera))
    .AddContent(new TooltipTextBlockData { Header = itemName, Body = itemDescription })
    .Show();
```

Key behaviour:
- Ignores raycasts so hover targets remain interactive.
- Follows its anchor by default and closes naturally when the pointer exits (you control this in your own MonoBehaviour).
- Rejects action payloads—if you add actions they are ignored and a warning is logged so the UX stays predictable.

### Context Menus (`ContextMenuBuilder`)
Use this builder when the player performs an explicit action (click, button press, long-press) and you want to surface interactive buttons.

```csharp
void OnPointerClick(PointerEventData eventData)
{
    var context = TooltipPlayerContextUtility.FromEventSystem(eventData.eventSystem, eventData.pressEventCamera);

    var handle = new ContextMenuBuilder()
        .WithAnchor(buttonRect, follow: false)
        .WithOffset(new Vector2(0f, 18f))
        .WithPivot(new Vector2(0.5f, 0f))
        .WithPlayerContext(context)
        .AddContent(new TooltipTextBlockData { Header = "Actions", Body = "Select an option:" })
        .AddAction(new TooltipActionData("Inspect", (h, ctx) => Inspect(item)))
        .AddAction(new TooltipActionData("Equip", (h, ctx) => Equip(item)))
        .AddAction(new TooltipActionData("Drop", (h, ctx) => Drop(item)))
        .Show();

    // Keep a reference to close the menu from elsewhere if needed.
    _menuHandle = handle;
}
```

Context menu defaults:
- Sticky + raycast-blocking so buttons receive input until you close them via `handle.Close()` or another action.
- Actions remain reusable (set `closeOnTrigger: false`) to keep menus open while responding to clicks.
- Co-op ready: pass a `TooltipPlayerContext` for each user so inputs are filtered per-player. You can also build a `TooltipRequest` manually, set `PresentationMode = TooltipPresentationMode.ContextMenu`, and call `TooltipSystemRoot.Instance.ShowContextMenu(request)` if you prefer structs over builders.

### Shared Handle Operations
Regardless of mode, the returned `TooltipHandle` lets you:
- Update placement via `handle.UpdateAnchor(...)`, `UpdateWorldPosition(...)`, or `UpdateOffset(...)`.
- Refresh the visual payload (`handle.ReplaceContent(newContent, newActions)`), ideal for context menu state changes.
- Close the presentation explicitly with `handle.Close()` when the owning UI element disappears.

### Choosing the Right Mode
- Pick `TooltipBuilder` for glanceable information: hover hints, stat readouts, “press to inspect” prompts.
- Pick `ContextMenuBuilder` for deliberate interactions: inventory context menus, radial actions, multi-step workflows.
- Never call `ShowTooltip` with action lists—route actionable payloads through `ShowContextMenu`/`ContextMenuBuilder` to guarantee consistent behaviour.
## 8. Player Context & Input Routing

`TooltipPlayerContext` restricts interaction to the player who opened the tooltip. Populate it with references relevant to your input stack:

- `TooltipPlayerContextUtility.FromPlayerInput(playerInput, uiCamera)` – recommended when using the Input System with `PlayerInput` + `MultiplayerEventSystem`.
- `TooltipPlayerContextUtility.FromEventSystem(eventSystem, uiCamera, optionalPlayerIndex)` – for legacy EventSystem setups.

The context stores references to `PlayerInput`, `InputSystemUIInputModule`, `MultiplayerEventSystem`, or a plain `EventSystem`. `TooltipActionButtonView` consults this context in `MatchesEvent` to decide whether the incoming event belongs to the owner.

## 9. Creating Custom Content Types

1. Derive a new payload from `TooltipContentData` with the fields you need (e.g., stats, progress bars, socket grids).
2. Create a view by inheriting from `TooltipContentView<TPayload>`.
3. Implement `Bind(TPayload data, TooltipBindingContext context)` to populate visuals. Use `context.Handle`, `context.System`, and `context.PlayerContext` to react to user interaction.
4. Create a prefab wired to the new view component.
5. Register the prefab in `TooltipSystemRoot._contentPrefabs` or via `RegisterContentPrefab` at runtime.

Example payload + view snippet:

```csharp
[Serializable]
public sealed class TooltipProgressData : TooltipContentData
{
    public string Label;
    public float NormalizedValue;
}

public sealed class TooltipProgressView : TooltipContentView<TooltipProgressData>
{
    [SerializeField] private TextMeshProUGUI _label;
    [SerializeField] private Image _fill;

    protected override void Bind(TooltipProgressData data, TooltipBindingContext context)
    {
        _label.text = data.Label;
        _fill.fillAmount = Mathf.Clamp01(data.NormalizedValue);
    }
}
```

## 10. Building Context Menus & Action Lists

Actions are defined by `TooltipActionData`. Each action can show an icon, a label, and executes an `UnityAction<TooltipHandle, TooltipPlayerContext>` when triggered. Set `closeOnTrigger` to `false` to keep the tooltip open after the action fires.

```csharp
var dismantle = new TooltipActionData(
    label: "Dismantle",
    callback: (handle, ctx) => DismantleItem(itemId),
    icon: dismantleSprite,
    interactable: playerHasPermission,
    closeOnTrigger: true);
```

Add as many actions as needed when assembling the request. They will spawn under `_actionsRoot` if assigned, otherwise as part of the main layout.

## 11. Managing Multiple Tooltips

- The system supports multiple simultaneous tooltips; each call to `ShowTooltip` spawns a pooled `TooltipView`.
- Use the `Tag` field in `TooltipRequest` to mark ownership (e.g., item reference), enabling you to close or update specific tooltips later.
- `FollowTarget = true` lets tooltips track moving anchors. Use `UpdateAnchor` or `UpdateWorldPosition` on the handle to switch follow targets at runtime.


## 12. Advanced Notes

- **PlayerTooltipController**: Optional per-player facade that tracks hover tooltips and context menus. Reuse a single instance across shop, inventory, and weapon UIs to keep ownership and player filtering consistent.
- **Pooling**: `TooltipSystemRoot` reuses `TooltipView` instances. Reset transient state in TooltipView.Release or your custom content views to avoid leaking data.
- **Sorting**: Use `TooltipRequest.SortingOffset` to adjust per-tooltip canvas sorting order if you stack multiple UIs.
- **Clamping**: Enable/disable clamping globally via `TooltipSystemRoot.ClampToViewport` or per request (`TooltipRequest.ClampToViewport`).
- **Camera Override**: Provide a player-specific UI camera in `TooltipPlayerContext.UICamera` when using split-screen setups.

## 13. Debugging Checklist

- Missing prefab assignment? `TooltipSystemRoot` logs warnings when it cannot resolve content or action prefabs.
- Tooltip not visible? Verify the canvas group alpha, sorting order, and that the tooltip layer is inside the active canvas.
- Buttons responding to the wrong player? Ensure each player has its own `EventSystem`/`MultiplayerEventSystem` and that the correct context is passed into the request.

## 14. Quick Reference

- **Show tooltip**: `TooltipSystemRoot.Instance.ShowTooltip(request)`.
- **Close tooltip**: `handle.Close()`.
- **Update content**: `handle.ReplaceContent(newContent, newActions)`.
- **Register prefab at runtime**: `TooltipSystemRoot.Instance.RegisterContentPrefab(typeof(MyData), myViewPrefab)`.
- **Create context**: `TooltipPlayerContextUtility.FromPlayerInput(playerInput, uiCamera)`.

With these steps the tooltip system becomes reusable across the project and adaptable to new content or control schemes without modifying the core runtime code.

## 15. Example: Item Shop Integration

- Add `PlayerTooltipController` to the root of each player HUD (or reuse an existing one) and wire in the player's `MultiplayerEventSystem`/`PlayerInput` plus UI camera.
- Drag that controller into `ItemShopUI.tooltipController`; the slots register themselves in `Awake` and show hover tooltips automatically while purchase buttons call their assigned action directly.
- The `RuntimePlayerSpawner` now wires each player's `PlayerTooltipController` with the active `PlayerInput`, `MultiplayerEventSystem`, UI camera, and player index, so every screen (shop, inventory, weapons, build menus) receives the correct context automatically.
- Ensure your slot prefabs still use `ItemShopSlotUI` (implements pointer/select handlers) and that the shop canvas hosts a `TooltipSystemRoot` somewhere in the scene.
- When you reroll or close the shop the slots clear their tooltip/context menu handles, so the shared controller can immediately service other UI like inventory tabs or weapon loadouts.
- Customise labels/icons directly in `ItemShopSlotUI` or extend `PlayerTooltipController` with helper methods that build shared content blocks.
- Use `PlayerTooltipController.ShowContextMenu`/`ToggleContextMenu` from other UI (inventory, weapon loadouts, etc.) when you need explicit action menus; hover tooltips remain non-interactive.















