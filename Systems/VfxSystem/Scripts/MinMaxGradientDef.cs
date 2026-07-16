using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JG.Vfx
{
    public enum MinMaxGradientMode
    {
        Color,
        TwoColors,
        Gradient,
        TwoGradients
    }

    [Serializable]
    public class GradientColorKeyDef
    {
        public Color color = UnityEngine.Color.white;
        public float time;
    }

    [Serializable]
    public class GradientAlphaKeyDef
    {
        public float alpha = 1f;
        public float time;
    }

    [Serializable]
    public class GradientDef
    {
        public List<GradientColorKeyDef> colorKeys = new();
        public List<GradientAlphaKeyDef> alphaKeys = new();

        public Gradient ToGradient()
        {
            var gradient = new Gradient();

            GradientColorKey[] cks;
            if (colorKeys != null && colorKeys.Count > 0)
            {
                cks = new GradientColorKey[colorKeys.Count];
                for (int i = 0; i < colorKeys.Count; i++)
                    cks[i] = new GradientColorKey(colorKeys[i].color, colorKeys[i].time);
            }
            else
            {
                cks = new[] { new GradientColorKey(UnityEngine.Color.white, 0f) };
            }

            GradientAlphaKey[] aks;
            if (alphaKeys != null && alphaKeys.Count > 0)
            {
                aks = new GradientAlphaKey[alphaKeys.Count];
                for (int i = 0; i < alphaKeys.Count; i++)
                    aks[i] = new GradientAlphaKey(alphaKeys[i].alpha, alphaKeys[i].time);
            }
            else
            {
                aks = new[] { new GradientAlphaKey(1f, 0f) };
            }

            gradient.SetKeys(cks, aks);
            return gradient;
        }
    }

    /// <summary>
    /// JSON-friendly mirror of <see cref="ParticleSystem.MinMaxGradient"/>.
    /// Accepts shorthand in JSON: a color string ("#RRGGBBAA") or color array becomes Color mode.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(MinMaxGradientDefConverter))]
    public class MinMaxGradientDef
    {
        public MinMaxGradientMode mode = MinMaxGradientMode.Color;
        public Color color = UnityEngine.Color.white;
        public Color colorMin = UnityEngine.Color.white;
        public Color colorMax = UnityEngine.Color.white;
        public GradientDef gradient;
        public GradientDef gradientMin;
        public GradientDef gradientMax;

        public ParticleSystem.MinMaxGradient ToMinMaxGradient()
        {
            switch (mode)
            {
                case MinMaxGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(colorMin, colorMax);
                case MinMaxGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient((gradient ?? new GradientDef()).ToGradient());
                case MinMaxGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        (gradientMin ?? new GradientDef()).ToGradient(),
                        (gradientMax ?? new GradientDef()).ToGradient());
                default:
                    return new ParticleSystem.MinMaxGradient(color);
            }
        }

        public static MinMaxGradientDef FromColor(Color c) =>
            new() { mode = MinMaxGradientMode.Color, color = c };
    }

    public sealed class MinMaxGradientDefConverter : JsonConverter<MinMaxGradientDef>
    {
        public override bool CanWrite => false;

        public override MinMaxGradientDef ReadJson(JsonReader reader, Type objectType, MinMaxGradientDef existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.String:
                case JsonToken.StartArray:
                {
                    // Delegate color parsing to the serializer's Color converter
                    // (hex strings, named colors, [r,g,b,a] arrays).
                    var c = serializer.Deserialize<Color>(reader);
                    return MinMaxGradientDef.FromColor(c);
                }
                case JsonToken.StartObject:
                {
                    var obj = JObject.Load(reader);
                    var instance = new MinMaxGradientDef();
                    using var objReader = obj.CreateReader();
                    serializer.Populate(objReader, instance);
                    return instance;
                }
                default:
                    throw new JsonSerializationException(
                        $"Cannot parse MinMaxGradient from token '{reader.TokenType}'.");
            }
        }

        public override void WriteJson(JsonWriter writer, MinMaxGradientDef value, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }
}
