using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JGameFramework.UI.Tooltips
{
    public sealed class TooltipImageBlockView : TooltipContentView<TooltipImageBlockData>
    {
        [SerializeField] private Image _image;
        [SerializeField] private TextMeshProUGUI _caption;
        [SerializeField] private LayoutElement _layoutElement;

        protected override void Bind(TooltipImageBlockData data, TooltipBindingContext context)
        {
            if (_image != null)
            {
                _image.sprite = data.Sprite;
                _image.preserveAspect = data.PreserveAspect;
                _image.enabled = data.Sprite != null;

                if (_layoutElement != null)
                {
                    _layoutElement.preferredWidth = data.PreferredSize.x;
                    _layoutElement.preferredHeight = data.PreferredSize.y;
                }
            }

            if (_caption != null)
            {
                bool showCaption = !string.IsNullOrWhiteSpace(data.Caption);
                _caption.gameObject.SetActive(showCaption);
                if (showCaption)
                {
                    _caption.richText = true;
                    _caption.text = data.Caption;
                    _caption.alignment = data.CaptionAlignment;
                    _caption.color = data.CaptionColor;
                    if (data.CaptionFont) _caption.font = data.CaptionFont;
                }
            }
        }
    }
}
