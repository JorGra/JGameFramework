using System.Collections.Generic;
using JG.Scaling;
using UnityEngine;

namespace JG.GameContent.Tooltips
{
    /// <summary>
    /// One renderable stat row in an item tooltip. Carries both the base
    /// (no-stats) value and the current (with-stats) value so callers can
    /// format static, delta, or current-only displays. Also feeds the
    /// {cur:id} / {base:id} / {delta:id} token expander.
    /// </summary>
    public struct TooltipStatLine
    {
        /// <summary>Stable id used by description tokens (e.g. "damage", "speed_bonus").</summary>
        public string Id;

        /// <summary>Display label, typically a stat name or convention header.</summary>
        public string Label;

        /// <summary>Pre-modifier / no-stats value.</summary>
        public float BaseValue;

        /// <summary>Final value after live stats are applied.</summary>
        public float CurrentValue;

        /// <summary>C# composite format string applied to the value, e.g. "{0:0.##}" or "{0:P0}".</summary>
        public string Format;

        /// <summary>Multiplier applied to the raw value before formatting (e.g. 100 for 0..1 percent stats).</summary>
        public float DisplayScale;

        /// <summary>If true, current &gt; base renders as the positive color. False inverts.</summary>
        public bool HigherIsBetter;

        /// <summary>If true, the row is hidden from the tooltip body but still resolves tokens.</summary>
        public bool Hidden;

        /// <summary>Optional scaling terms backing this line's value (for ScaledValue-derived lines).</summary>
        public IReadOnlyList<ScalingTerm> ScalingTerms;

        public TooltipStatLine(string id, string label, float baseValue, float currentValue,
                               string format = "{0:0.##}", float displayScale = 1f,
                               bool higherIsBetter = true, bool hidden = false,
                               IReadOnlyList<ScalingTerm> scalingTerms = null)
        {
            Id = id;
            Label = label;
            BaseValue = baseValue;
            CurrentValue = currentValue;
            Format = string.IsNullOrEmpty(format) ? "{0}" : format;
            DisplayScale = displayScale == 0f ? 1f : displayScale;
            HigherIsBetter = higherIsBetter;
            Hidden = hidden;
            ScalingTerms = scalingTerms;
        }
    }
}
