using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JG.GameContent
{
    /// <summary>
    /// Generic discriminator-based converter for polymorphic hierarchies.
    /// Requires a discriminator property (default "type"). Scans for non-abstract subclasses
    /// of TBase in loaded assemblies and maps discriminator values using the subclass's TypeId property
    /// (if present) or class name.
    /// </summary>
    public sealed class DiscriminatorConverter<TBase> : JsonConverter<TBase>
        where TBase : class
    {
        private readonly string _discriminator;
        private static readonly Dictionary<string, Type> TypeMap = new(StringComparer.OrdinalIgnoreCase);
        private static bool _bootstrapped;

        public DiscriminatorConverter() : this("type") { }

        public DiscriminatorConverter(string discriminator = "type")
        {
            _discriminator = string.IsNullOrWhiteSpace(discriminator) ? "type" : discriminator;
        }

        public override TBase ReadJson(JsonReader reader, Type objectType, TBase existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = JObject.Load(reader);
            EnsureBootstrapped();

            var typeId = obj[_discriminator]?.ToString();
            if (string.IsNullOrWhiteSpace(typeId))
                throw new JsonSerializationException($"{typeof(TBase).Name} missing '{_discriminator}'.");

            if (!TypeMap.TryGetValue(typeId, out var concrete))
                throw new JsonSerializationException($"Unknown {typeof(TBase).Name} type '{typeId}'. Known: {string.Join(",", TypeMap.Keys)}");

            // Remove discriminator before populating to avoid MissingMemberHandling errors
            obj.Remove(_discriminator);

            var instance = (TBase)Activator.CreateInstance(concrete);
            using var objReader = obj.CreateReader();
            serializer.Populate(objReader, instance);
            return instance;
        }

        public override void WriteJson(JsonWriter writer, TBase value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var jObj = JObject.FromObject(value, serializer);
            var typeId = GetTypeId(value.GetType());
            jObj[_discriminator] = typeId;
            jObj.WriteTo(writer);
        }

        private static void EnsureBootstrapped()
        {
            if (_bootstrapped)
                return;

            _bootstrapped = true;
            TypeMap.Clear();

            var baseType = typeof(TBase);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException rtlEx)
                {
                    types = rtlEx.Types;
                }
                catch
                {
                    continue;
                }

                if (types == null)
                    continue;

                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || !baseType.IsAssignableFrom(t))
                        continue;

                    var id = GetTypeId(t);
                    if (!TypeMap.ContainsKey(id))
                        TypeMap[id] = t;
                }
            }
        }

        private static string GetTypeId(Type t)
        {
            if (t == null)
                return string.Empty;

            try
            {
                var prop = t.GetProperty("TypeId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                if (prop != null && typeof(string).IsAssignableFrom(prop.PropertyType))
                {
                    var instance = Activator.CreateInstance(t);
                    var value = prop.GetValue(instance) as string;
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch { }

            return t.Name;
        }
    }
}
