using System;
using System.Collections.Generic;
using UnityEngine;

namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Describes how a tooltip should be presented.
    /// </summary>
    public struct TooltipRequest
    {
        public RectTransform Anchor;
        public Vector3? WorldPosition;
        public Vector2? ScreenPosition;
        public Vector2 Pivot;
        public Vector2 Offset;
        public bool FollowTarget;
        public TooltipPlayerContext PlayerContext;
        public IReadOnlyList<TooltipContentData> Content;
        public IReadOnlyList<TooltipActionData> Actions;
        public int SortingOffset;
        public bool? ClampToViewport;
        public object Tag;

        public TooltipRequest(
            IReadOnlyList<TooltipContentData> content,
            TooltipPlayerContext playerContext,
            RectTransform anchor = null,
            Vector3? worldPosition = null,
            Vector2? screenPosition = null,
            Vector2? offset = null,
            IReadOnlyList<TooltipActionData> actions = null,
            Vector2? pivot = null,
            bool followTarget = true,
            bool? clampToViewport = null,
            int sortingOffset = 0,
            object tag = null)
        {
            Content = content;
            PlayerContext = playerContext;
            Anchor = anchor;
            WorldPosition = worldPosition;
            ScreenPosition = screenPosition;
            Offset = offset ?? Vector2.zero;
            Actions = actions;
            Pivot = pivot ?? new Vector2(0f, 1f);
            FollowTarget = followTarget;
            ClampToViewport = clampToViewport;
            SortingOffset = sortingOffset;
            Tag = tag;
        }

        public bool HasExplicitScreenPosition => ScreenPosition.HasValue;
        public bool HasWorldPosition => WorldPosition.HasValue;
        public bool HasAnchor => Anchor != null;
    }
}
