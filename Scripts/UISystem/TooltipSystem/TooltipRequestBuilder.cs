using System.Collections.Generic;
using UnityEngine;

namespace JGameFramework.UI.Tooltips
{
    public abstract class TooltipBuilderBase<TBuilder> where TBuilder : TooltipBuilderBase<TBuilder>
    {
        protected readonly List<TooltipContentData> _content = new();
        protected readonly List<TooltipActionData> _actions = new();
        protected TooltipRequest _request;
        private readonly bool _allowsActions;

        protected TooltipBuilderBase(TooltipPresentationMode mode, bool allowsActions)
        {
            _allowsActions = allowsActions;
            _request = default;
            _request.PresentationMode = mode;
            _request.Offset = Vector2.zero;
            _request.Pivot = default;
            _request.FollowTarget = mode == TooltipPresentationMode.Tooltip;

            if (mode == TooltipPresentationMode.ContextMenu)
            {
                _request.BlocksRaycasts = true;
                _request.Sticky = true;
                _request.FollowTarget = false;
            }
        }

        protected TBuilder Self => (TBuilder)this;

        public TBuilder WithAnchor(RectTransform anchor, bool follow = true)
        {
            _request.Anchor = anchor;
            _request.WorldPosition = null;
            _request.ScreenPosition = null;
            _request.FollowTarget = follow;
            return Self;
        }

        public TBuilder WithWorldPosition(Vector3 position, bool follow = false)
        {
            _request.WorldPosition = position;
            _request.Anchor = null;
            _request.ScreenPosition = null;
            _request.FollowTarget = follow;
            return Self;
        }

        public TBuilder WithScreenPosition(Vector2 screenPosition)
        {
            _request.ScreenPosition = screenPosition;
            _request.Anchor = null;
            _request.WorldPosition = null;
            _request.FollowTarget = false;
            return Self;
        }

        public TBuilder WithFollowTarget(bool follow)
        {
            _request.FollowTarget = follow;
            return Self;
        }

        public TBuilder WithPlayerContext(TooltipPlayerContext context)
        {
            _request.PlayerContext = context;
            return Self;
        }

        public TBuilder WithPivot(Vector2 pivot)
        {
            _request.Pivot = pivot;
            return Self;
        }

        public TBuilder WithOffset(Vector2 offset)
        {
            _request.Offset = offset;
            return Self;
        }

        public TBuilder WithSortingOffset(int sortingOffset)
        {
            _request.SortingOffset = sortingOffset;
            return Self;
        }

        public TBuilder WithClampOverride(bool clamp)
        {
            _request.ClampToViewport = clamp;
            return Self;
        }

        public TBuilder WithBlocksRaycasts(bool blocksRaycasts)
        {
            _request.BlocksRaycasts = blocksRaycasts;
            return Self;
        }

        public TBuilder AsStickyTooltip(bool sticky = true)
        {
            _request.Sticky = sticky;
            return Self;
        }

        public TBuilder WithTag(object tag)
        {
            _request.Tag = tag;
            return Self;
        }

        public TBuilder AddContent(TooltipContentData content)
        {
            if (content != null)
            {
                _content.Add(content);
            }
            return Self;
        }

        protected void AddActionInternal(TooltipActionData action)
        {
            if (!_allowsActions)
            {
                Debug.LogWarning("This builder does not allow actions. Use ContextMenuBuilder instead.");
                return;
            }

            if (action != null)
            {
                _actions.Add(action);
            }
        }

        public TooltipHandle Show()
        {
            return ShowInternal();
        }

        public TooltipRequest Build()
        {
            return BuildInternal();
        }

        public void Reset()
        {
            var mode = _request.PresentationMode;
            _content.Clear();
            _actions.Clear();
            _request = default;
            _request.PresentationMode = mode;
            _request.Offset = Vector2.zero;
            _request.Pivot = default;
            _request.FollowTarget = mode == TooltipPresentationMode.Tooltip;

            if (mode == TooltipPresentationMode.ContextMenu)
            {
                _request.BlocksRaycasts = true;
                _request.Sticky = true;
                _request.FollowTarget = false;
            }
        }

        protected TooltipHandle ShowInternal()
        {
            _request.Content = _content;
            _request.Actions = _allowsActions ? _actions : null;
            EnsurePivot();
            return TooltipSystemRoot.Instance.ShowPresentation(_request);
        }

        protected TooltipRequest BuildInternal()
        {
            _request.Content = _content;
            _request.Actions = _allowsActions ? _actions : null;
            EnsurePivot();
            return _request;
        }

        private void EnsurePivot()
        {
            if (_request.Pivot == default)
            {
                _request.Pivot = new Vector2(0f, 1f);
            }
        }
    }

    public sealed class TooltipBuilder : TooltipBuilderBase<TooltipBuilder>
    {
        public TooltipBuilder() : base(TooltipPresentationMode.Tooltip, allowsActions: false)
        {
        }
    }

    public sealed class ContextMenuBuilder : TooltipBuilderBase<ContextMenuBuilder>
    {
        public ContextMenuBuilder() : base(TooltipPresentationMode.ContextMenu, allowsActions: true)
        {
        }

        public ContextMenuBuilder AddAction(TooltipActionData action)
        {
            AddActionInternal(action);
            return this;
        }

        public ContextMenuBuilder AddActions(IEnumerable<TooltipActionData> actions)
        {
            if (actions == null)
            {
                return this;
            }

            foreach (var action in actions)
            {
                AddActionInternal(action);
            }

            return this;
        }
    }
}
