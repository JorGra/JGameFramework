using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JG.Scaling;
using UnityEngine;

namespace JG.GameContent.Tooltips
{
    /// <summary>
    /// Engine-agnostic formatting helpers for <see cref="TooltipStatLine"/>.
    /// Lives in the framework so unit tests can reach it without touching
    /// project-specific stat definitions.
    /// </summary>
    public static class TooltipFormatting
    {
        public static readonly Color PositiveColor = new(0.55f, 0.95f, 0.55f);
        public static readonly Color NegativeColor = new(0.95f, 0.55f, 0.55f);
        public static readonly Color NeutralColor = Color.white;

        public static string FormatValue(in TooltipStatLine line, float value)
        {
            string fmt = string.IsNullOrEmpty(line.Format) ? "{0}" : line.Format;
            float scale = line.DisplayScale == 0f ? 1f : line.DisplayScale;
            return string.Format(CultureInfo.InvariantCulture, fmt, value * scale);
        }

        public static string FormatCurrent(in TooltipStatLine line) => FormatValue(line, line.CurrentValue);
        public static string FormatBase(in TooltipStatLine line) => FormatValue(line, line.BaseValue);

        public static string FormatDelta(in TooltipStatLine line)
        {
            if (Mathf.Approximately(line.BaseValue, line.CurrentValue))
                return FormatCurrent(line);

            var color = ResolveDeltaColor(in line);
            string baseStr = FormatBase(in line);
            string curStr = FormatCurrent(in line);
            string hex = ColorUtility.ToHtmlStringRGB(color);
            return $"{baseStr} <color=#{hex}>→ {curStr}</color>";
        }

        public static string FormatCurrentColored(in TooltipStatLine line)
        {
            string text = FormatCurrent(in line);
            if (Mathf.Approximately(line.BaseValue, line.CurrentValue)) return text;
            string hex = ColorUtility.ToHtmlStringRGB(ResolveDeltaColor(in line));
            return $"<color=#{hex}>{text}</color>";
        }

        /// <summary>
        /// Like <see cref="FormatCurrentColored"/> but always wraps in a color tag —
        /// positive when current is equal or improved by stats, negative when worsened.
        /// Used by description value tokens so authored numbers stay highlighted.
        /// </summary>
        public static string FormatCurrentHighlighted(in TooltipStatLine line)
        {
            string text = FormatCurrent(in line);
            Color color;
            if (Mathf.Approximately(line.BaseValue, line.CurrentValue))
            {
                color = PositiveColor;
            }
            else
            {
                color = ResolveDeltaColor(in line);
            }
            string hex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hex}>{text}</color>";
        }

        public static Color ResolveDeltaColor(in TooltipStatLine line)
        {
            if (Mathf.Approximately(line.BaseValue, line.CurrentValue)) return NeutralColor;
            bool currentHigher = line.CurrentValue > line.BaseValue;
            bool good = currentHigher == line.HigherIsBetter;
            return good ? PositiveColor : NegativeColor;
        }

        public static TooltipStatLine LineFromScaled(string id, string label, ScaledValue value,
                                                     IStatProvider stats,
                                                     string format = "{0:0.##}", float displayScale = 1f,
                                                     bool higherIsBetter = true, bool hidden = false)
        {
            float baseV = value.Base;
            float curV = stats != null ? value.Evaluate(stats) : baseV;
            var terms = value.HasScaling ? value.Scaling : null;
            return new TooltipStatLine(id, label, baseV, curV, format, displayScale, higherIsBetter, hidden, terms);
        }

        /// <summary>
        /// Builds " (<sprite name="stat">×factor ...)" for the given scaling terms.
        /// Returns empty when terms is null/empty. Skips zero-factor terms.
        /// </summary>
        public static string FormatScalingSuffix(IReadOnlyList<ScalingTerm> terms)
        {
            if (terms == null || terms.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            int written = 0;
            for (int i = 0; i < terms.Count; i++)
            {
                var t = terms[i];
                if (string.IsNullOrEmpty(t.Stat) || Mathf.Approximately(t.Factor, 0f)) continue;
                if (written > 0) sb.Append(' ');
                sb.Append("<sprite name=\"").Append(t.Stat).Append("\">×")
                  .Append(t.Factor.ToString("0.##", CultureInfo.InvariantCulture));
                written++;
            }
            if (written == 0) return string.Empty;
            return $" <color=#AAAAAA>({sb})</color>";
        }
    }
}
