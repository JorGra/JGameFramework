using System;

namespace JGameFramework.UI.Tooltips
{
    /// <summary>
    /// Abstraction for tooltip-capable items so tooltip UI stays project-agnostic.
    /// </summary>
    public interface IItemTooltipData
    {
        void BuildTooltip(ItemTooltipContext context, TooltipBuilder builder);
    }
}
