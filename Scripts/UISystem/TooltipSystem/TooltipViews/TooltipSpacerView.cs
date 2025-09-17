using UnityEngine;
using UnityEngine.UI;

namespace JGameFramework.UI.Tooltips
{
    public sealed class TooltipSpacerView : TooltipContentView<TooltipSpacerData>
    {
        [SerializeField] private LayoutElement _layoutElement;

        protected override void Bind(TooltipSpacerData data, TooltipBindingContext context)
        {
            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
                if (_layoutElement == null)
                {
                    _layoutElement = gameObject.AddComponent<LayoutElement>();
                }
            }

            _layoutElement.minHeight = data.Height;
            _layoutElement.preferredHeight = data.Height;
        }
    }
}
