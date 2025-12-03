using System;

namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Helper extensions for presenting shared item tooltip layouts across UI surfaces.
    /// </summary>
    public static class ItemTooltipExtensions
    {
        public static TooltipHandle ShowItemTooltip(
            this PlayerTooltipController controller,
            ItemTooltipContext context,
            Action<TooltipBuilder> configure = null)
        {
            if (controller == null || !context.HasItem)
            {
                return default;
            }

            return controller.ShowTooltip(
                owner: context.Owner ?? context.Item,
                anchor: context.Anchor,
                followTarget: context.FollowTarget,
                configure: builder =>
                {
                    context.ApplyPlacement(builder);
                    configure?.Invoke(builder);
                    context.Item.BuildTooltip(context, builder);
                },
                eventData: context.EventData,
                contextOverride: context.PlayerContextOverride);
        }

        public static TooltipBuilder BuildItemTooltip(
            this TooltipBuilder builder,
            ItemTooltipContext context)
        {
            if (builder == null || !context.HasItem)
            {
                return builder;
            }

            context.ApplyPlacement(builder);
            context.Item.BuildTooltip(context, builder);
            return builder;
        }
    }
}
