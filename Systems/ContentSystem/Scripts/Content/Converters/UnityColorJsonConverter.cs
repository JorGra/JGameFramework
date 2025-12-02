using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JG.GameContent
{
    /// <summary>
    /// Newtonsoft.Json converter that accepts multiple color formats:
    /// - "#RRGGBB", "#RRGGBBAA", "#RGB", "#RGBA"
    /// - Without leading '#': e.g. "FF00FF" or "FF00FFFF"
    /// - Named Unity colors: "white", "black", "red", etc.
    /// - Array: [r, g, b] or [r, g, b, a] in 0..1
    /// - Object: { "r": 1, "g": 0, "b": 0, "a": 1 }
    ///
    /// Writes as "#RRGGBBAA".
    /// </summary>
    public sealed class UnityColorJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(Color) || objectType == typeof(Color?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return objectType == typeof(Color?) ? (Color?)null : Color.clear;

            try
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var s = (reader.Value as string)?.Trim();
                    if (string.IsNullOrEmpty(s)) return Color.clear;

                    // Try Unity's HTML parser first (supports #RGB, #RRGGBB, #RGBA, #RRGGBBAA)
                    if (!s.StartsWith("#") && !s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        // If it looks like hex without '#', try prepending it first
                        if (IsLikelyHex(s) && ColorUtility.TryParseHtmlString("#" + s, out var hexCol))
                            return hexCol;
                    }
                    else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        var hex = s.Substring(2);
                        if (ColorUtility.TryParseHtmlString("#" + hex, out var col0x))
                            return col0x;
                    }

                    if (ColorUtility.TryParseHtmlString(s, out var col))
                        return col;

                    // Named colors fallback via UnityEngine.Color static properties
                    var named = TryParseNamedColor(s);
                    if (named.HasValue)
                        return named.Value;

                    throw new FormatException($"Unsupported color string '{s}'. Use #RRGGBB[AA] or named color.");
                }

                if (reader.TokenType == JsonToken.StartArray)
                {
                    var arr = JArray.Load(reader);
                    float r = arr.Count > 0 ? arr[0].Value<float>() : 0f;
                    float g = arr.Count > 1 ? arr[1].Value<float>() : 0f;
                    float b = arr.Count > 2 ? arr[2].Value<float>() : 0f;
                    float a = arr.Count > 3 ? arr[3].Value<float>() : 1f;
                    return new Color(r, g, b, a);
                }

                if (reader.TokenType == JsonToken.StartObject)
                {
                    var obj = JObject.Load(reader);
                    float r = obj.TryGetValue("r", StringComparison.OrdinalIgnoreCase, out var rv) ? rv.Value<float>() : 0f;
                    float g = obj.TryGetValue("g", StringComparison.OrdinalIgnoreCase, out var gv) ? gv.Value<float>() : 0f;
                    float b = obj.TryGetValue("b", StringComparison.OrdinalIgnoreCase, out var bv) ? bv.Value<float>() : 0f;
                    float a = obj.TryGetValue("a", StringComparison.OrdinalIgnoreCase, out var av) ? av.Value<float>() : 1f;
                    return new Color(r, g, b, a);
                }

                // Fallback: try default
                return serializer.Deserialize(reader, objectType);
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Failed to parse Unity Color: {ex.Message}");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var c = (Color)value;
            var r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
            var g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
            var b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
            var a = Mathf.Clamp(Mathf.RoundToInt(c.a * 255f), 0, 255);
            writer.WriteValue($"#{r:X2}{g:X2}{b:X2}{a:X2}");
        }

        private static bool IsLikelyHex(string s)
        {
            // 3,4,6,8 length hex patterns
            if (s.Length is 3 or 4 or 6 or 8)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    if (!Uri.IsHexDigit(s[i])) return false;
                }
                return true;
            }
            return false;
        }

        private static Color? TryParseNamedColor(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "black": return Color.black;
                case "blue": return Color.blue;
                case "clear": return Color.clear;
                case "cyan": return Color.cyan;
                case "gray":
                case "grey": return Color.gray;
                case "green": return Color.green;
                case "magenta": return Color.magenta;
                case "red": return Color.red;
                case "white": return Color.white;
                case "yellow": return Color.yellow;
                default: return null;
            }
        }
    }
}

