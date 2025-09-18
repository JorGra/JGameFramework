using System.Collections.Generic;
using UnityEngine;

namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Fluent helper that makes it easier to create tooltips at runtime.
    /// </summary>
    public sealed class TooltipRequestBuilder
    {
        private readonly List<TooltipContentData> _content = new();
        private readonly List<TooltipActionData> _actions = new();
        private TooltipRequest _request;

        public TooltipRequestBuilder WithAnchor(RectTransform anchor, bool follow = true)
        {
            _request.Anchor = anchor;
            _request.WorldPosition = null;
            _request.ScreenPosition = null;
            _request.FollowTarget = follow;
            return this;
        }

        public TooltipRequestBuilder WithWorldPosition(Vector3 position, bool follow = false)
        {
            _request.WorldPosition = position;
            _request.Anchor = null;
            _request.ScreenPosition = null;
            _request.FollowTarget = follow;
            return this;
        }

        public TooltipRequestBuilder WithScreenPosition(Vector2 screenPosition)
        {
            _request.ScreenPosition = screenPosition;
            _request.Anchor = null;
            _request.WorldPosition = null;
            _request.FollowTarget = false;
            return this;
        }

        public TooltipRequestBuilder WithFollowTarget(bool follow)
        {
            _request.FollowTarget = follow;
            return this;
        }

        public TooltipRequestBuilder WithPlayerContext(TooltipPlayerContext context)
        {
            _request.PlayerContext = context;
            return this;
        }

        public TooltipRequestBuilder WithPivot(Vector2 pivot)
        {
            _request.Pivot = pivot;
            return this;
        }

        public TooltipRequestBuilder WithOffset(Vector2 offset)
        {
            _request.Offset = offset;
            return this;
        }

        public TooltipRequestBuilder WithSortingOffset(int sortingOffset)
        {
            _request.SortingOffset = sortingOffset;
            return this;
        }

        public TooltipRequestBuilder WithClampOverride(bool clamp)
        {
            _request.ClampToViewport = clamp;
            return this;
        }

        public TooltipRequestBuilder WithBlocksRaycasts(bool blocksRaycasts)
        {
            _request.BlocksRaycasts = blocksRaycasts;
            return this;
        }

        public TooltipRequestBuilder AsStickyTooltip(bool sticky = true)
        {
            _request.Sticky = sticky;
            return this;
        }

        public TooltipRequestBuilder WithTag(object tag)
        {
            _request.Tag = tag;
            return this;
        }

        public TooltipRequestBuilder AddContent(TooltipContentData content)
        {
            if (content != null)
            {
                _content.Add(content);
            }
            return this;
        }

        public TooltipRequestBuilder AddAction(TooltipActionData action)
        {
            if (action != null)
            {
                _actions.Add(action);
            }
            return this;
        }

        public TooltipHandle Show()
        {
            _request.Content = _content;
            _request.Actions = _actions;
            EnsurePivot();
            return TooltipSystemRoot.Instance.ShowTooltip(_request);
        }

        public TooltipRequest Build()
        {
            _request.Content = _content;
            _request.Actions = _actions;
            EnsurePivot();
            return _request;
        }

        public void Reset()
        {
            _content.Clear();
            _actions.Clear();
            _request = default;
        }

        private void EnsurePivot()
        {
            if (_request.Pivot == default)
            {
                _request.Pivot = new Vector2(0f, 1f);
            }
        }
    }
}


