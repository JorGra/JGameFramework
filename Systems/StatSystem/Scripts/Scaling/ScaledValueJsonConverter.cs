using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JG.Scaling
{
    public sealed class ScaledValueJsonConverter : JsonConverter<ScaledValue>
    {
        public override ScaledValue ReadJson(JsonReader reader, Type objectType, ScaledValue existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            switch (token.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    return new ScaledValue(token.Value<float>());
                case JTokenType.Null:
                    return new ScaledValue(0f);
                case JTokenType.Object:
                {
                    var obj = (JObject)token;
                    float b = obj.TryGetValue("base", StringComparison.OrdinalIgnoreCase, out var bt)
                        ? bt.Value<float>()
                        : 0f;

                    var terms = new List<ScalingTerm>();
                    if (obj.TryGetValue("scaling", StringComparison.OrdinalIgnoreCase, out var st) && st is JArray arr)
                    {
                        foreach (var item in arr)
                        {
                            if (!(item is JObject row)) continue;
                            string stat = row.TryGetValue("stat", StringComparison.OrdinalIgnoreCase, out var s) ? s.Value<string>() : null;
                            float factor = row.TryGetValue("factor", StringComparison.OrdinalIgnoreCase, out var f) ? f.Value<float>() : 0f;
                            ScalingMode mode = ScalingMode.Sum;
                            if (row.TryGetValue("mode", StringComparison.OrdinalIgnoreCase, out var m))
                            {
                                var name = m.Value<string>();
                                if (!string.IsNullOrEmpty(name) && !Enum.TryParse(name, true, out mode))
                                {
                                    throw new JsonSerializationException($"Unknown ScalingMode '{name}'.");
                                }
                            }
                            terms.Add(new ScalingTerm(stat, mode, factor));
                        }
                    }
                    return new ScaledValue(b, terms);
                }
                default:
                    throw new JsonSerializationException($"Cannot convert {token.Type} to ScaledValue.");
            }
        }

        public override void WriteJson(JsonWriter writer, ScaledValue value, JsonSerializer serializer)
        {
            if (!value.HasScaling)
            {
                writer.WriteValue(value.Base);
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("base");
            writer.WriteValue(value.Base);
            writer.WritePropertyName("scaling");
            writer.WriteStartArray();
            foreach (var t in value.Scaling)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("stat");
                writer.WriteValue(t.Stat);
                writer.WritePropertyName("mode");
                writer.WriteValue(t.Mode.ToString());
                writer.WritePropertyName("factor");
                writer.WriteValue(t.Factor);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
