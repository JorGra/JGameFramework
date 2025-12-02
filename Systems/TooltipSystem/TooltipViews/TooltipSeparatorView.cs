using UnityEngine;
using UnityEngine.UI;


namespace JGameFramework.UI.Tooltips
{
    public class TooltipSeparatorView : TooltipContentView<TooltipSeparatorData>
    {
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private Image _image;

        protected override void Bind(TooltipSeparatorData data, TooltipBindingContext context)
        {
            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
                if (_layoutElement == null)
                {
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
                }
            }

            if (_image != null && data.Color != default)
            {
                _image.color = data.Color;
            }

            _layoutElement.minHeight = data.Height;
            _layoutElement.preferredHeight = data.Height;
        }
    }
}