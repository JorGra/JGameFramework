using UnityEngine;
using UnityEngine.EventSystems;

namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Runtime metadata passed to item tooltip builders so they can tailor
    /// presentation without relying on concrete UI implementations.
    /// </summary>
    public readonly struct ItemTooltipContext
    {
        public ItemTooltipContext(
            object owner,
            IItemTooltipData item,
            RectTransform anchor,
            BaseEventData eventData,
            Vector2 fallbackOffset,
            bool followTarget = true,
            bool clampToScreen = true,
            TooltipPlayerContext? playerContextOverride = null)
        {
            Owner = owner;
            Item = item;
            Anchor = anchor;
            EventData = eventData;
            FallbackOffset = fallbackOffset;
            FollowTarget = followTarget;
            ClampToScreen = clampToScreen;
            PlayerContextOverride = playerContextOverride;
        }

        public object Owner { get; }
        public IItemTooltipData Item { get; }
        public RectTransform Anchor { get; }
        public BaseEventData EventData { get; }
        public Vector2 FallbackOffset { get; }
        public bool FollowTarget { get; }
        public bool ClampToScreen { get; }
        public TooltipPlayerContext? PlayerContextOverride { get; }

        public bool HasItem => Item != null;
        public bool HasAnchor => Anchor != null;
        public bool IsPointerEvent => EventData is PointerEventData;
        public bool IsNavigationEvent => !IsPointerEvent && EventData != null;

        public Vector2 ResolveOffset()
        {
            return ResolveOffset(FallbackOffset);
        }

        public Vector2 ResolveOffset(Vector2 fallbackOffset)
        {
            if (!HasAnchor || !IsNavigationEvent)
            {
                return fallbackOffset;
            }

            var direction = GuessDirectionFromAnchor(fallbackOffset);
            if (direction == Vector2.zero)
            {
                return fallbackOffset;
            }

            var resolved = new Vector2(
                Mathf.Abs(fallbackOffset.x),
                Mathf.Abs(fallbackOffset.y));

            resolved.x *= direction.x >= 0f ? 1f : -1f;
            resolved.y *= direction.y >= 0f ? 1f : -1f;

            if (Mathf.Approximately(resolved.x, 0f) && Mathf.Approximately(resolved.y, 0f))
            {
                return fallbackOffset;
            }

            return resolved;
        }

        private Vector2 GuessDirectionFromAnchor(Vector2 fallbackOffset)
        {
            var direction = new Vector2(
                Mathf.Approximately(fallbackOffset.x, 0f) ? 0f : Mathf.Sign(fallbackOffset.x),
                Mathf.Approximately(fallbackOffset.y, 0f) ? 0f : Mathf.Sign(fallbackOffset.y));

            if (!HasAnchor)
            {
                return direction == Vector2.zero ? Vector2.one : direction;
            }

            var canvas = Anchor.GetComponentInParent<Canvas>();
            Camera camera = null;
            if (canvas != null)
            {
                switch (canvas.renderMode)
                {
                    case RenderMode.ScreenSpaceCamera:
                        camera = canvas.worldCamera;
                        break;
                    case RenderMode.WorldSpace:
                        camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
                        break;
                }
            }

            var rect = Anchor.rect;
            var worldCenter = Anchor.TransformPoint(rect.center);
            var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldCenter);

            var signX = screenPoint.x >= Screen.width * 0.5f ? -1f : 1f;
            if (Mathf.Approximately(direction.x, 0f))
            {
                direction.x = signX;
            }
            else
            {
                direction.x = Mathf.Sign(direction.x) * Mathf.Abs(signX);
            }

            if (Mathf.Approximately(direction.y, 0f))
            {
                direction.y = 1f;
            }

            return direction;
        }

        public void ApplyPlacement(TooltipBuilder builder)
        {
            if (builder == null)
            {
                return;
            }

            if (ClampToScreen)
            {
                builder.WithClampOverride(true);
            }

            builder.WithOffset(ResolveOffset());
        }
    }
}
