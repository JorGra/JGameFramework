using TMPro;
using UnityEngine;

namespace JGameFramework.UI.Tooltips
{
    public sealed class TooltipKeyValueRowView : TooltipContentView<TooltipKeyValueRowData>
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private TextMeshProUGUI _value;

        protected override void Bind(TooltipKeyValueRowData data, TooltipBindingContext context)
        {
            if (_label != null)
            {
                _label.text = data.Label ?? string.Empty;
                _label.color = data.LabelColor;
                if (data.LabelFont) _label.font = data.LabelFont;
            }

            if (_value != null)
            {
                _value.text = data.Value ?? string.Empty;
                _value.color = data.ValueColor;
                if (data.ValueFont) _value.font = data.ValueFont;
                //_value.alignment = data.Alignment;
            }
        }
    }
}
