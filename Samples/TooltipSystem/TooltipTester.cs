using JGameFramework.UI.Tooltips;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTester : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TooltipType
    {
        SimpleText,
        Image,
        Actions
    }

    public TooltipType tooltipType = TooltipType.SimpleText;

    private TooltipHandle handle;
    public Sprite sprite;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipType != TooltipType.Actions)
        {
            handle.Close();
        }
    }

    private void ShowTooltip()
    {
        var builder = new TooltipRequestBuilder()
            .WithAnchor(GetComponent<RectTransform>(), follow: true);

        switch (tooltipType)
        {
            case TooltipType.SimpleText:
                builder.AddContent(new TooltipTextBlockData { Header = "Hello", Body = "Tooooltip" });
                break;
            case TooltipType.Image:
                builder.AddContent(new TooltipImageBlockData { Sprite = sprite });
                break;
            case TooltipType.Actions:
                builder.AsStickyTooltip();

                var actions = new List<TooltipActionData>
                {
                    new TooltipActionData("Action 1", (h, ctx) => Debug.Log("Action 1 executed"), closeOnTrigger: false),
                    new TooltipActionData("Action 2", (h, ctx) => Debug.Log("Action 2 executed"), closeOnTrigger: false),
                    new TooltipActionData("Action 3", (h, ctx) => Debug.Log("Action 3 executed"))
                };

                foreach (var action in actions)
                {
                    builder.AddAction(action);
                }
                break;
        }

        handle = builder.Show();
    }
}
