/// Credit Simie
/// Sourced from - http://forum.unity3d.com/threads/flowlayoutgroup.296709/
/// Example http://forum.unity3d.com/threads/flowlayoutgroup.296709/
/// Update by Martin Sharkbomb - http://forum.unity3d.com/threads/flowlayoutgroup.296709/#post-1977028
/// Last item alignment fix by Vicente Russo - https://bitbucket.org/SimonDarksideJ/unity-ui-extensions/issues/22/flow-layout-group-align
/// Vertical Flow by Ramon Molossi 

using System.Collections.Generic;
using System.Text;

namespace UnityEngine.UI.Extensions
{
    /// <summary>
    /// Layout Group controller that arranges children in bars, fitting as many on a line until total size exceeds parent bounds
    /// </summary>
    [AddComponentMenu("Layout/Extensions/Flow Layout Group")]
    public class FlowLayoutGroup : LayoutGroup
    {
        public enum Axis { Horizontal = 0, Vertical = 1 }

        private float _layoutHeight;
        private float _layoutWidth;

        public float SpacingX = 0f;
        public float SpacingY = 0f;
        public bool ExpandHorizontalSpacing = false;
        public bool ChildForceExpandWidth = false;
        public bool ChildForceExpandHeight = false;
        public bool invertOrder = false;

        [SerializeField]
        protected Axis m_StartAxis = Axis.Horizontal;

        public Axis StartAxis { get { return m_StartAxis; } set { SetProperty(ref m_StartAxis, value); } }

        public override void CalculateLayoutInputHorizontal()
        {
            if (StartAxis == Axis.Horizontal)
            {
                base.CalculateLayoutInputHorizontal();
            }
            else
            {
                CalcAlongAxis(0, true);
            }
        }

        public override void CalculateLayoutInputVertical()
        {
            if (StartAxis == Axis.Horizontal)
            {
                CalcAlongAxis(1, true);
            }
            else
            {
                CalcAlongAxis(1, false);
            }
        }

        public override void SetLayoutHorizontal()
        {
            if (StartAxis == Axis.Horizontal)
            {
                SetChildrenAlongAxis(0, true);
            }
            else
            {
                SetChildrenAlongAxis(0, false);
            }
        }

        public override void SetLayoutVertical()
        {
            if (StartAxis == Axis.Horizontal)
            {
                SetChildrenAlongAxis(1, true);
            }
            else
            {
                SetChildrenAlongAxis(1, false);
            }
        }

        private void CalcAlongAxis(int axis, bool isVertical)
        {
            float combinedPadding = axis == 0 ? padding.horizontal : padding.vertical;
            float totalMin = combinedPadding;
            float totalPreferred = combinedPadding;
            float totalFlexible = 0;

            float size = rectTransform.rect.size[axis];
            float innerSize = size - combinedPadding;
            float pos = (axis == 0 ? padding.left : padding.top);

            _layoutHeight = 0f;
            _layoutWidth = 0f;

            float maxPrimary = 0f;
            float maxSecondary = 0f;

            var children = invertOrder ? GetChildOrderReversed() : GetChildOrder();

            foreach (var child in children)
            {
                float min = LayoutUtility.GetMinSize(child, axis);
                float preferred = LayoutUtility.GetPreferredSize(child, axis);
                float flexible = LayoutUtility.GetFlexibleSize(child, axis);

                float minSecondary = LayoutUtility.GetMinSize(child, axis == 0 ? 1 : 0);
                float preferredSecondary = LayoutUtility.GetPreferredSize(child, axis == 0 ? 1 : 0);

                float requiredSpace = Mathf.Clamp(preferred, min, flexible > 0 ? size : preferred);

                if (pos + requiredSpace > innerSize + 0.01f)
                {
                    totalPreferred = Mathf.Max(totalPreferred, pos + combinedPadding);
                    pos = (axis == 0 ? padding.left : padding.top);

                    totalMin += maxPrimary + (axis == 0 ? SpacingY : SpacingX);
                    _layoutHeight += maxSecondary + SpacingY;
                    maxPrimary = 0f;
                    maxSecondary = 0f;
                }

                pos += requiredSpace + (axis == 0 ? SpacingX : SpacingY);
                maxPrimary = Mathf.Max(maxPrimary, requiredSpace);
                maxSecondary = Mathf.Max(maxSecondary, preferredSecondary);
            }

            totalMin += maxPrimary;
            totalPreferred = Mathf.Max(totalPreferred, pos + combinedPadding);
            _layoutWidth = Mathf.Max(_layoutWidth, totalPreferred);
            _layoutHeight += maxSecondary + (axis == 0 ? padding.vertical : padding.horizontal);

            if (axis == 0)
            {
                SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
            }
            else
            {
                SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
            }
        }

        private void SetChildrenAlongAxis(int axis, bool isVertical)
        {
            float size = rectTransform.rect.size[axis];
            float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
            float pos = (axis == 0 ? padding.left : padding.top);
            float crossPos = (axis == 0 ? padding.top : padding.left);

            float lineMaxSecondary = 0f;
            var children = invertOrder ? GetChildOrderReversed() : GetChildOrder();

            foreach (var child in children)
            {
                float min = LayoutUtility.GetMinSize(child, axis);
                float preferred = LayoutUtility.GetPreferredSize(child, axis);
                float flexible = LayoutUtility.GetFlexibleSize(child, axis);

                float minSecondary = LayoutUtility.GetMinSize(child, axis == 0 ? 1 : 0);
                float preferredSecondary = LayoutUtility.GetPreferredSize(child, axis == 0 ? 1 : 0);

                float requiredSpace = Mathf.Clamp(preferred, min, flexible > 0 ? size : preferred);

                if (pos + requiredSpace > innerSize + 0.01f)
                {
                    pos = (axis == 0 ? padding.left : padding.top);
                    crossPos += lineMaxSecondary + (axis == 0 ? SpacingY : SpacingX);
                    lineMaxSecondary = 0f;
                }

                float alignedSecondary = crossPos;
                if (ChildForceExpandHeight && axis == 0)
                {
                    preferredSecondary = innerSize;
                }
                if (ChildForceExpandWidth && axis == 1)
                {
                    preferred = innerSize;
                }

                if (axis == 0)
                {
                    SetChildAlongAxis(child, 0, pos, preferred);
                    SetChildAlongAxis(child, 1, alignedSecondary, preferredSecondary);
                }
                else
                {
                    SetChildAlongAxis(child, 1, pos, preferred);
                    SetChildAlongAxis(child, 0, alignedSecondary, preferredSecondary);
                }

                pos += requiredSpace + (axis == 0 ? SpacingX : SpacingY);
                lineMaxSecondary = Mathf.Max(lineMaxSecondary, preferredSecondary);
            }
        }

        private List<RectTransform> GetChildOrder()
        {
            var children = new List<RectTransform>();
            for (int i = 0; i < rectTransform.childCount; i++)
            {
                var child = rectTransform.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy) continue;
                children.Add(child);
            }
            return children;
        }

        private List<RectTransform> GetChildOrderReversed()
        {
            var children = GetChildOrder();
            children.Reverse();
            return children;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"SpacingX: {SpacingX}, SpacingY: {SpacingY}");
            sb.AppendLine($"StartAxis: {StartAxis}, ChildForceExpandWidth: {ChildForceExpandWidth}, ChildForceExpandHeight: {ChildForceExpandHeight}");
            return sb.ToString();
        }
    }
}
