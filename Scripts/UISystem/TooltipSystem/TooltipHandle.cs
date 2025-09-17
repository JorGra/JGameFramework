using System.Collections.Generic;
using UnityEngine;

namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Lightweight token that lets you mutate or close a shown tooltip.
    /// </summary>
    public readonly struct TooltipHandle
    {
        private readonly TooltipSystemRoot _system;
        internal readonly TooltipView View;

        internal TooltipHandle(TooltipSystemRoot system, TooltipView view)
        {
            _system = system;
            View = view;
        }

        public bool IsValid => _system != null && View != null;
        public TooltipPlayerContext PlayerContext => View != null ? View.PlayerContext : default;
        public object Tag => View != null ? View.Tag : null;

        public void Close()
        {
            if (!IsValid) return;
            _system.DismissTooltip(this);
        }

        public void ReplaceContent(IReadOnlyList<TooltipContentData> content, IReadOnlyList<TooltipActionData> actions = null)
        {
            if (!IsValid) return;
            View.ReplaceContent(content, actions);
        }

        public void UpdateOffset(Vector2 offset)
        {
            if (!IsValid) return;
            View.UpdateOffset(offset);
        }

        public void UpdateAnchor(RectTransform anchor, bool followTarget = true)
        {
            if (!IsValid) return;
            View.UpdateAnchor(anchor, followTarget);
        }

        public void UpdateWorldPosition(Vector3 worldPosition, bool followTarget = true)
        {
            if (!IsValid) return;
            View.UpdateWorldPosition(worldPosition, followTarget);
        }

        public void SetVisibility(bool visible)
        {
            if (!IsValid) return;
            View.SetVisibility(visible);
        }
    }
}
