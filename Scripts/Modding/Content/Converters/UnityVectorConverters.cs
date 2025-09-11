using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JG.GameContent
{
    /// <summary>
    /// Accepts [x,y], {"x":..,"y":..}, or single number v (mapped to [v,v]).
    /// </summary>
    public sealed class UnityVector2JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(Vector2) || objectType == typeof(Vector2?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return objectType == typeof(Vector2?) ? (Vector2?)null : Vector2.zero;

            if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
            {
                float v = Convert.ToSingle(reader.Value);
                return new Vector2(v, v);
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                var arr = JArray.Load(reader);
                float x = arr.Count > 0 ? arr[0].Value<float>() : 0f;
                float y = arr.Count > 1 ? arr[1].Value<float>() : 0f;
                return new Vector2(x, y);
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                var obj = JObject.Load(reader);
                float x = obj.TryGetValue("x", StringComparison.OrdinalIgnoreCase, out var xv) ? xv.Value<float>() : 0f;
                float y = obj.TryGetValue("y", StringComparison.OrdinalIgnoreCase, out var yv) ? yv.Value<float>() : 0f;
                return new Vector2(x, y);
            }

            throw new JsonSerializationException($"Unsupported token {reader.TokenType} for Vector2");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var v = (Vector2)value;
            writer.WriteStartArray();
            writer.WriteValue(v.x);
            writer.WriteValue(v.y);
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Accepts [x,y,z], {"x":..,"y":..,"z":..}, or single number v (mapped to [v,v,v]).
    /// </summary>
    public sealed class UnityVector3JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(Vector3) || objectType == typeof(Vector3?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return objectType == typeof(Vector3?) ? (Vector3?)null : Vector3.zero;

            if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
            {
                float v = Convert.ToSingle(reader.Value);
                return new Vector3(v, v, v);
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                var arr = JArray.Load(reader);
                float x = arr.Count > 0 ? arr[0].Value<float>() : 0f;
                float y = arr.Count > 1 ? arr[1].Value<float>() : 0f;
                float z = arr.Count > 2 ? arr[2].Value<float>() : 0f;
                return new Vector3(x, y, z);
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                var obj = JObject.Load(reader);
                float x = obj.TryGetValue("x", StringComparison.OrdinalIgnoreCase, out var xv) ? xv.Value<float>() : 0f;
                float y = obj.TryGetValue("y", StringComparison.OrdinalIgnoreCase, out var yv) ? yv.Value<float>() : 0f;
                float z = obj.TryGetValue("z", StringComparison.OrdinalIgnoreCase, out var zv) ? zv.Value<float>() : 0f;
                return new Vector3(x, y, z);
            }

            throw new JsonSerializationException($"Unsupported token {reader.TokenType} for Vector3");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var v = (Vector3)value;
            writer.WriteStartArray();
            writer.WriteValue(v.x);
            writer.WriteValue(v.y);
            writer.WriteValue(v.z);
            writer.WriteEndArray();
        }
    }
}

