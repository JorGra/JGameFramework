using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JGameFramework.UI.Tooltips
{
    public sealed class TooltipKeyValueRowView : TooltipContentView<TooltipKeyValueRowData>
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private TextMeshProUGUI _value;
        [SerializeField, Min(0f), Tooltip("Minimum gap between label and value.")]
        private float _minGap = 12f;

        protected override void Bind(TooltipKeyValueRowData data, TooltipBindingContext context)
        {
            if (_label != null)
            {
                _label.text = data.Label ?? string.Empty;
                _label.color = data.LabelColor;
                if (data.LabelFont) _label.font = data.LabelFont;
                PinLabelWidth();
            }

            if (_value != null)
            {
                TMProUtilities.EnableWrapping(_value);
                _value.text = data.Value ?? string.Empty;
                _value.color = data.ValueColor;
                if (data.ValueFont) _value.font = data.ValueFont;
                //_value.alignment = data.Alignment;
                GetOrAddLayoutElement(_value).flexibleWidth = 1f;
            }

            var row = GetComponent<HorizontalLayoutGroup>();
            if (row != null)
            {
                row.spacing = Mathf.Max(row.spacing, _minGap);
            }
        }

        // The row's HorizontalLayoutGroup shrinks children proportionally when space is
        // tight, which wrapped short labels ("<icon> Damage") onto two lines. Keep the
        // label on one line at its full width and let only the value wrap.
        private void PinLabelWidth()
        {
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;

            float width = _label.GetPreferredValues(_label.text, float.PositiveInfinity, float.PositiveInfinity).x;
            var element = GetOrAddLayoutElement(_label);
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }

        private static LayoutElement GetOrAddLayoutElement(Component target)
        {
            var element = target.GetComponent<LayoutElement>();
            return element != null ? element : target.gameObject.AddComponent<LayoutElement>();
        }
    }
}
