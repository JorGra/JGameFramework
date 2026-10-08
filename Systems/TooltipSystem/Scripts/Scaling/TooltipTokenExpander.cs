using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JG.GameContent.Tooltips
{
    /// <summary>
    /// Replaces <c>{cur:id}</c>, <c>{base:id}</c>, and <c>{delta:id}</c> tokens
    /// in tooltip description bodies with formatted values pulled from a
    /// <see cref="TooltipStatLine"/> map. Unknown ids render the literal token
    /// and emit a one-time warning.
    /// <para>
    /// Also resolves the semantic spans <c>&lt;pos&gt;…&lt;/pos&gt;</c> and
    /// <c>&lt;neg&gt;…&lt;/neg&gt;</c> into color tags using
    /// <see cref="TooltipFormatting.PositiveColor"/> / <see cref="TooltipFormatting.NegativeColor"/>,
    /// so content never hardcodes the palette. <c>{cur:id}</c> tokens inside a span
    /// take the span's color. Regular rich-text color tags pass through untouched.
    /// </para>
    /// </summary>
    public static class TooltipTokenExpander
    {
        // {kind:id} where kind ∈ {cur, base, delta} and id is alphanum/dot/underscore.
        static readonly Regex TokenRegex = new(
            @"\{(?<kind>cur|base|delta):(?<id>[\w.]+)\}",
            RegexOptions.Compiled);

        // <pos>…</pos> / <neg>…</neg>; no nesting.
        static readonly Regex SpanRegex = new(
            @"<(?<tag>pos|neg)>(?<body>.*?)</\k<tag>>",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        static readonly Regex StrayTagRegex = new(@"</?(pos|neg)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string Expand(string source, IReadOnlyDictionary<string, TooltipStatLine> lines)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            var sb = new StringBuilder(source.Length + 32);
            int cursor = 0;
            foreach (Match span in SpanRegex.Matches(source))
            {
                sb.Append(ExpandTokens(source.Substring(cursor, span.Index - cursor), lines, insideSpan: false));

                bool positive = string.Equals(span.Groups["tag"].Value, "pos", System.StringComparison.OrdinalIgnoreCase);
                string hex = ColorUtility.ToHtmlStringRGB(positive ? TooltipFormatting.PositiveColor : TooltipFormatting.NegativeColor);
                sb.Append("<color=#").Append(hex).Append('>')
                  .Append(ExpandTokens(span.Groups["body"].Value, lines, insideSpan: true))
                  .Append("</color>");

                cursor = span.Index + span.Length;
            }
            sb.Append(ExpandTokens(source.Substring(cursor), lines, insideSpan: false));
            return sb.ToString();
        }

        static string ExpandTokens(string source, IReadOnlyDictionary<string, TooltipStatLine> lines, bool insideSpan)
        {
            if (string.IsNullOrEmpty(source) || lines == null || lines.Count == 0)
                return source;

            return TokenRegex.Replace(source, match =>
            {
                string id = match.Groups["id"].Value;
                string kind = match.Groups["kind"].Value;

                if (!lines.TryGetValue(id, out var line))
                {
                    Debug.LogWarning($"[TooltipTokenExpander] Unknown stat line id '{id}' in token '{match.Value}'.");
                    return match.Value;
                }

                return kind switch
                {
                    // Inside a span the span color wins; outside, values stay highlighted.
                    "cur" => (insideSpan ? TooltipFormatting.FormatCurrent(in line) : TooltipFormatting.FormatCurrentHighlighted(in line))
                           + TooltipFormatting.FormatScalingSuffix(line.ScalingTerms),
                    "base" => TooltipFormatting.FormatBase(in line)
                           + TooltipFormatting.FormatScalingSuffix(line.ScalingTerms),
                    "delta" => TooltipFormatting.FormatDelta(in line)
                           + TooltipFormatting.FormatScalingSuffix(line.ScalingTerms),
                    _ => match.Value,
                };
            });
        }

        /// <summary>
        /// Returns a problem description when <c>&lt;pos&gt;</c>/<c>&lt;neg&gt;</c> tags are
        /// unbalanced (they would otherwise render as literal text), or null when fine.
        /// </summary>
        public static string ValidateSpans(string source)
        {
            if (string.IsNullOrEmpty(source)) return null;

            // Nested tags inside a span body render literally.
            foreach (Match span in SpanRegex.Matches(source))
            {
                var nested = StrayTagRegex.Match(span.Groups["body"].Value);
                if (nested.Success) return $"Nested '{nested.Value}' tag inside <{span.Groups["tag"].Value}>.";
            }

            // Strip well-formed spans, then any remaining tag is stray.
            var stray = StrayTagRegex.Match(SpanRegex.Replace(source, string.Empty));
            return stray.Success ? $"Unbalanced '{stray.Value}' tag." : null;
        }
    }
}
