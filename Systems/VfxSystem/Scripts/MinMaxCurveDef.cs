using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JG.Vfx
{
    public enum MinMaxCurveMode
    {
        Constant,
        TwoConstants,
        Curve,
        TwoCurves
    }

    [Serializable]
    public class CurveKeyDef
    {
        public float time;
        public float value;
        public float inTangent;
        public float outTangent;
    }

    [Serializable]
    public class CurveDef
    {
        public List<CurveKeyDef> keys = new();

        public AnimationCurve ToAnimationCurve()
        {
            if (keys == null || keys.Count == 0)
                return AnimationCurve.Constant(0f, 1f, 1f);

            var frames = new Keyframe[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                frames[i] = new Keyframe(k.time, k.value, k.inTangent, k.outTangent);
            }
            return new AnimationCurve(frames);
        }
    }

    /// <summary>
    /// JSON-friendly mirror of <see cref="ParticleSystem.MinMaxCurve"/>.
    /// Accepts shorthand in JSON: a plain number becomes Constant mode,
    /// a two-element array becomes TwoConstants mode.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(MinMaxCurveDefConverter))]
    public class MinMaxCurveDef
    {
        public MinMaxCurveMode mode = MinMaxCurveMode.Constant;
        public float constant;
        public float constantMin;
        public float constantMax;
        public float curveMultiplier = 1f;
        public CurveDef curve;
        public CurveDef curveMin;
        public CurveDef curveMax;

        public ParticleSystem.MinMaxCurve ToMinMaxCurve()
        {
            switch (mode)
            {
                case MinMaxCurveMode.TwoConstants:
                    return new ParticleSystem.MinMaxCurve(constantMin, constantMax);
                case MinMaxCurveMode.Curve:
                    return new ParticleSystem.MinMaxCurve(curveMultiplier, (curve ?? new CurveDef()).ToAnimationCurve());
                case MinMaxCurveMode.TwoCurves:
                    return new ParticleSystem.MinMaxCurve(curveMultiplier,
                        (curveMin ?? new CurveDef()).ToAnimationCurve(),
                        (curveMax ?? new CurveDef()).ToAnimationCurve());
                default:
                    return new ParticleSystem.MinMaxCurve(constant);
            }
        }

        public static MinMaxCurveDef FromConstant(float value) =>
            new() { mode = MinMaxCurveMode.Constant, constant = value };
    }

    public sealed class MinMaxCurveDefConverter : JsonConverter<MinMaxCurveDef>
    {
        public override bool CanWrite => false;

        public override MinMaxCurveDef ReadJson(JsonReader reader, Type objectType, MinMaxCurveDef existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.Integer:
                case JsonToken.Float:
                    return MinMaxCurveDef.FromConstant(Convert.ToSingle(reader.Value));
                case JsonToken.StartArray:
                {
                    var arr = JArray.Load(reader);
                    if (arr.Count != 2)
                        throw new JsonSerializationException(
                            $"MinMaxCurve array shorthand needs exactly 2 elements [min, max], got {arr.Count}.");
                    return new MinMaxCurveDef
                    {
                        mode = MinMaxCurveMode.TwoConstants,
                        constantMin = arr[0].Value<float>(),
                        constantMax = arr[1].Value<float>()
                    };
                }
                case JsonToken.StartObject:
                {
                    var obj = JObject.Load(reader);
                    var instance = new MinMaxCurveDef();
                    using var objReader = obj.CreateReader();
                    serializer.Populate(objReader, instance);
                    return instance;
                }
                default:
                    throw new JsonSerializationException(
                        $"Cannot parse MinMaxCurve from token '{reader.TokenType}'.");
            }
        }

        public override void WriteJson(JsonWriter writer, MinMaxCurveDef value, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }
}
