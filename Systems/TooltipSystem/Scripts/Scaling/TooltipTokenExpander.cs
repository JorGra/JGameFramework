using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JG.GameContent.Tooltips
{
    /// <summary>
    /// Replaces <c>{cur:id}</c>, <c>{base:id}</c>, and <c>{delta:id}</c> tokens
    /// in tooltip description bodies with formatted values pulled from a
    /// <see cref="TooltipStatLine"/> map. Unknown ids render the literal token
    /// and emit a one-time warning.
    /// </summary>
    public static class TooltipTokenExpander
    {
        // {kind:id} where kind ∈ {cur, base, delta} and id is alphanum/dot/underscore.
        static readonly Regex TokenRegex = new(
            @"\{(?<kind>cur|base|delta):(?<id>[\w.]+)\}",
            RegexOptions.Compiled);

        public static string Expand(string source, IReadOnlyDictionary<string, TooltipStatLine> lines)
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
                    "cur" => TooltipFormatting.FormatCurrentHighlighted(in line)
                           + TooltipFormatting.FormatScalingSuffix(line.ScalingTerms),
                    "base" => TooltipFormatting.FormatBase(in line)
                           + TooltipFormatting.FormatScalingSuffix(line.ScalingTerms),
                    "delta" => TooltipFormatting.FormatDelta(in line)
                           + TooltipFormatting.FormatScalingSuffix(line.ScalingTerms),
                    _ => match.Value,
                };
            });
        }
    }
}
