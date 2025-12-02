using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JGameFramework.UI.Tooltips
{
    public sealed class TooltipTextBlockView : TooltipContentView<TooltipTextBlockData>
    {
        [SerializeField] private TextMeshProUGUI _header;
        [SerializeField] private TextMeshProUGUI _body;
        [SerializeField] private VerticalLayoutGroup _layoutGroup;

        protected override void Bind(TooltipTextBlockData data, TooltipBindingContext context)
        {
            if (_layoutGroup != null)
            {
                _layoutGroup.spacing = data.Spacing;
            }

            if (_header != null)
            {
                bool showHeader = data.ShowHeader && !string.IsNullOrWhiteSpace(data.Header);
                _header.gameObject.SetActive(showHeader);

                if (showHeader)
                {
                    _header.richText = data.UseRichText;
                    _header.text = data.UseRichText ? data.Header : TMProUtilities.ToPlainText(data.Header);
                    _header.alignment = data.HeaderAlignment;
                    _header.color = data.HeaderColor;
                    if (data.HeaderFont) _header.font = data.HeaderFont;
                }
            }

            if (_body != null)
            {
                string body = data.Body ?? string.Empty;
                _body.richText = data.UseRichText;
                _body.gameObject.SetActive(body.Length > 0);
                _body.text = data.UseRichText ? body : TMProUtilities.ToPlainText(body);
                _body.alignment = data.BodyAlignment;
                _body.color = data.BodyColor;
                if (data.BodyFont) _body.font = data.BodyFont;
            }
        }
    }
}
