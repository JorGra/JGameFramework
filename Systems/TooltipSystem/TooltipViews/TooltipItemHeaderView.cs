using JGameFramework.UI.Tooltips;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TooltipItemHeaderView : TooltipContentView<TooltipItemHeaderBlockData>
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI caption;

    protected override void Bind(TooltipItemHeaderBlockData data, TooltipBindingContext context)
    {
        if (icon != null)
        {
            icon.sprite = data.Sprite;
            icon.gameObject.SetActive(data.Sprite != null);
        }
        if (title != null)
        {
            title.text = data.Title ?? string.Empty;
        }
        if (caption != null)
        {
            caption.text = data.Caption ?? string.Empty;
        }
    }
}
