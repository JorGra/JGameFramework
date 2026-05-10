using System.Collections.Generic;
using JG.Scaling;

namespace JG.GameContent.Tooltips
{
    /// <summary>
    /// Implemented by item effects (or item defs) that want to contribute custom
    /// tooltip rows beyond what the convention auto-emits.
    /// Provider is null when no live stats are available; implementations
    /// should fall back to base values in that case.
    /// </summary>
    public interface ITooltipStatLineProvider
    {
        IEnumerable<TooltipStatLine> GetTooltipLines(IStatProvider stats);
    }
}
