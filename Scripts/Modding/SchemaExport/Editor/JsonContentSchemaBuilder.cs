#if UNITY_EDITOR
using JG.GameContent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

namespace JG.GameContent.SchemaExport
{
    internal sealed class JsonContentSchemaBuilder
    {
        private readonly Dictionary<Type, string> _definitionKeys = new();
        private readonly Dictionary<string, JObject> _definitions = new(StringComparer.Ordinal);
        private readonly JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        });

        public JObject Build(Type rootType, string contentFolder)
        {
            _definitionKeys.Clear();
            _definitions.Clear();

            ScriptableObject instance = null;
            try
            {
                instance = ScriptableObject.CreateInstance(rootType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JsonContentSchemaBuilder] Unable to create instance of {rootType.FullName}: {ex.Message}");
            }

            var rootSchema = BuildObjectSchema(rootType, instance);

            var document = new JObject
            {
                [""] = "https://json-schema.org/draft/2020-12/schema",
                [""] = $"jg://schema/{rootType.FullName}",
                ["title"] = rootType.Name,
                ["type"] = "object",
                ["properties"] = rootSchema["properties"] ?? new JObject(),
                ["additionalProperties"] = false,
                ["x-content-folder"] = contentFolder,
                ["x-assembly-qualified-name"] = rootType.AssemblyQualifiedName,
                ["x-display-name"] = ObjectNames.NicifyVariableName(rootType.Name)
            };

            if (rootSchema.TryGetValue("required", out var requiredToken) && requiredToken is JArray req && req.Count > 0)
                document["required"] = req;

            if (_definitions.Count > 0)
            {
                var defs = new JObject();
                foreach (var kvp in _definitions.OrderBy(p => p.Key, StringComparer.Ordinal))
                    defs[kvp.Key] = kvp.Value;
                document[""] = defs;
            }

            if (instance != null)
                ScriptableObject.DestroyImmediate(instance);

            return document;
        }

        private JObject BuildObjectSchema(Type type, object instance)
        {
            var properties = new JObject();
            var required = new JArray();

            var fields = EnumerateSerializableFields(type).ToList();
            var assetBindingsByMember = BuildAssetBindingsByMember(type);
            var assetBindingsByKey = BuildAssetBindingsByKey(assetBindingsByMember);

            foreach (var field in fields)
            {
                var value = instance != null ? field.GetValue(instance) : null;
                var schema = BuildSchemaForField(field, value, assetBindingsByMember, assetBindingsByKey);
                if (schema == null)
                    continue;

                var jsonName = field.Name;
                properties[jsonName] = schema;

                if (IsFieldRequired(field))
                    required.Add(jsonName);
            }

            var obj = new JObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["additionalProperties"] = false
            };

            if (required.Count > 0)
                obj["required"] = required;

            return obj;
        }

        private static IEnumerable<FieldInfo> EnumerateSerializableFields(Type type)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var current = type;
            while (current != null && current != typeof(ScriptableObject) && current != typeof(UnityEngine.Object))
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (var field in current.GetFields(flags))
                {
                    if (field.IsStatic || field.IsInitOnly)
                        continue;
                    if (field.IsDefined(typeof(NonSerializedAttribute), true))
                        continue;
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true))
                        continue;
                    if (!seen.Add(field.Name))
                        continue;

                    yield return field;
                }
                current = current.BaseType;
            }
        }

        private static Dictionary<string, List<AssetBindingInfo>> BuildAssetBindingsByMember(Type type)
        {
            var map = new Dictionary<string, List<AssetBindingInfo>>(StringComparer.Ordinal);
            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (var member in members)
            {
                var attr = member.GetCustomAttribute<AssetFromPathAttribute>();
                if (attr == null)
                    continue;

                if (!map.TryGetValue(member.Name, out var list))
                {
                    list = new List<AssetBindingInfo>();
                    map[member.Name] = list;
                }
                list.Add(new AssetBindingInfo(member.Name, attr.PathKey, attr.Optional));
            }
            return map;
        }

        private static Dictionary<string, List<AssetBindingInfo>> BuildAssetBindingsByKey(Dictionary<string, List<AssetBindingInfo>> byMember)
        {
            var map = new Dictionary<string, List<AssetBindingInfo>>(StringComparer.OrdinalIgnoreCase);
            foreach (var list in byMember.Values)
            {
                foreach (var binding in list)
                {
                    if (string.IsNullOrWhiteSpace(binding.PathKey))
                        continue;
                    if (!map.TryGetValue(binding.PathKey, out var keyList))
                    {
                        keyList = new List<AssetBindingInfo>();
                        map[binding.PathKey] = keyList;
                    }
                    keyList.Add(binding);
                }
            }
            return map;
        }

        private JObject BuildSchemaForField(FieldInfo field,
                                            object value,
                                            Dictionary<string, List<AssetBindingInfo>> assetBindingsByMember,
                                            Dictionary<string, List<AssetBindingInfo>> assetBindingsByKey)
        {
            var schema = BuildSchemaForType(field.FieldType, value);
            if (schema == null)
                return null;

            schema["title"] = ObjectNames.NicifyVariableName(field.Name);

            ApplyDocumentation(schema, field);
            ApplyValidationAttributes(schema, field);
            ApplyCustomMetadata(schema, field, assetBindingsByMember, assetBindingsByKey);
            ApplyDefaultValue(schema, value);

            return schema;
        }

        private JObject BuildSchemaForType(Type type, object value)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return new JObject
                {
                    ["type"] = "string",
                    ["readOnly"] = true,
                    ["x-unity-object-type"] = type.FullName
                };
            }

            if (type == typeof(string))
                return new JObject { ["type"] = "string" };

            if (type == typeof(bool))
                return new JObject { ["type"] = "boolean" };

            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                type == typeof(uint) || type == typeof(ushort) || type == typeof(ulong))
                return new JObject { ["type"] = "integer" };

            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return new JObject { ["type"] = "number" };

            if (type.IsEnum)
                return BuildEnumSchema(type, value);

            if (type == typeof(Vector2) || type == typeof(Vector2Int))
                return BuildVectorSchema(2, value, type == typeof(Vector2Int) ? "integer" : "number");
            if (type == typeof(Vector3) || type == typeof(Vector3Int))
                return BuildVectorSchema(3, value, type == typeof(Vector3Int) ? "integer" : "number");
            if (type == typeof(Color))
                return BuildColorSchema(value);

            if (IsListType(type, out var elementType))
                return BuildArraySchema(type, elementType, value);

            if (type.IsArray)
                return BuildArraySchema(type, type.GetElementType(), value);

            if (IsSerializableClassOrStruct(type))
                return BuildReferenceSchema(type, value);

            Debug.LogWarning($"[JsonContentSchemaBuilder] Unsupported field type {type.FullName}. It will be treated as a free-form object.");
            return new JObject { ["type"] = "object" };
        }

        private JObject BuildEnumSchema(Type enumType, object currentValue)
        {
            var names = Enum.GetNames(enumType);
            var schema = new JObject
            {
                ["type"] = "string",
                ["enum"] = new JArray(names)
            };

            var enriched = new JArray();
            foreach (var name in names)
            {
                var numeric = Convert.ChangeType(Enum.Parse(enumType, name), Enum.GetUnderlyingType(enumType));
                enriched.Add(new JObject
                {
                    ["name"] = name,
                    ["value"] = JToken.FromObject(numeric)
                });
            }
            schema["x-enum-values"] = enriched;

            if (currentValue != null)
                schema["default"] = currentValue.ToString();

            return schema;
        }

        private JObject BuildVectorSchema(int dimensions, object value, string numberType)
        {
            var itemsSchema = new JObject { ["type"] = numberType };
            var arraySchema = new JObject
            {
                ["type"] = "array",
                ["minItems"] = dimensions,
                ["maxItems"] = dimensions,
                ["items"] = itemsSchema,
                ["x-unity-vector"] = new JObject { ["dimensions"] = dimensions, ["numberType"] = numberType }
            };

            if (value != null)
            {
                try
                {
                    arraySchema["default"] = value switch
                    {
                        Vector2 v2 => new JArray(v2.x, v2.y),
                        Vector3 v3 => new JArray(v3.x, v3.y, v3.z),
                        Vector2Int vi2 => new JArray(vi2.x, vi2.y),
                        Vector3Int vi3 => new JArray(vi3.x, vi3.y, vi3.z),
                        _ => JToken.FromObject(value, _serializer)
                    };
                }
                catch { }
            }

            return arraySchema;
        }

        private JObject BuildColorSchema(object value)
        {
            var schema = new JObject
            {
                ["type"] = "string",
                ["format"] = "color",
                ["x-unity-color-formats"] = new JArray("#RRGGBB", "#RRGGBBAA", "named", "array", "object")
            };

            if (value is Color color)
            {
                if (ColorUtility.ToHtmlStringRGBA(color) is { } rgba)
                    schema["default"] = "#" + rgba;
            }

            return schema;
        }

        private JObject BuildArraySchema(Type collectionType, Type elementType, object value)
        {
            var itemsSchema = BuildSchemaForType(elementType ?? typeof(object), null) ?? new JObject();
            var schema = new JObject
            {
                ["type"] = "array",
                ["items"] = itemsSchema,
                ["x-unity-collection"] = collectionType.IsArray ? "array" : collectionType.FullName
            };

            if (value is IList list && list.Count > 0)
            {
                try
                {
                    schema["default"] = JToken.FromObject(list, _serializer);
                }
                catch { }
            }

            return schema;
        }

        private JObject BuildReferenceSchema(Type complexType, object value)
        {
            if (!_definitionKeys.TryGetValue(complexType, out var key))
            {
                key = SanitizeDefinitionKey(complexType);
                _definitionKeys[complexType] = key;

                var placeholder = new JObject();
                _definitions[key] = placeholder;

                object instance = value;
                bool destroy = false;
                if (instance == null)
                    instance = CreateInstance(complexType, out destroy);

                var schema = BuildObjectSchema(complexType, instance);
                schema["title"] = complexType.Name;
                schema["x-unity-type"] = complexType.AssemblyQualifiedName;

                _definitions[key] = schema;

                if (destroy && instance is UnityEngine.Object unityObject)
                    UnityEngine.Object.DestroyImmediate(unityObject);
            }

            return new JObject { [""] = $"#//{key}" };
        }

        private static bool IsListType(Type type, out Type elementType)
        {
            elementType = null;
            if (!typeof(IEnumerable).IsAssignableFrom(type))
                return false;

            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }

            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments();
                if (args.Length == 1)
                {
                    elementType = args[0];
                    return true;
                }
            }

            return false;
        }

        private static bool IsSerializableClassOrStruct(Type type)
        {
            if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return false;
            if (type.IsValueType)
                return true;
            return type.IsClass && (type.IsSerializable || type.GetCustomAttribute<SerializableAttribute>() != null);
        }

        private static object CreateInstance(Type type, out bool destroyUnityObject)
        {
            destroyUnityObject = false;
            try
            {
                if (typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    destroyUnityObject = true;
                    return ScriptableObject.CreateInstance(type);
                }

                if (type.IsValueType)
                    return Activator.CreateInstance(type);

                if (type.GetConstructor(Type.EmptyTypes) != null)
                    return Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JsonContentSchemaBuilder] Failed to create instance of {type.FullName}: {ex.Message}");
            }

            try
            {
                return FormatterServices.GetUninitializedObject(type);
            }
            catch
            {
                return null;
            }
        }

        private static string SanitizeDefinitionKey(Type type)
        {
            var raw = type.FullName ?? type.Name;
            var chars = raw.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_').ToArray();
            return new string(chars);
        }

        private static bool IsFieldRequired(FieldInfo field)
            => string.Equals(field.Name, "id", StringComparison.OrdinalIgnoreCase);

        private void ApplyDocumentation(JObject schema, FieldInfo field)
        {
            if (field.GetCustomAttribute<TooltipAttribute>() is TooltipAttribute tooltip && !string.IsNullOrWhiteSpace(tooltip.tooltip))
                schema["description"] = tooltip.tooltip;

            if (field.GetCustomAttribute<HeaderAttribute>() is HeaderAttribute header && !string.IsNullOrWhiteSpace(header.header))
                schema["x-unity-header"] = header.header;

            if (field.GetCustomAttribute<TextAreaAttribute>() is TextAreaAttribute textArea)
            {
                schema["x-unity-text-area"] = new JObject
                {
                    ["minLines"] = textArea.minLines,
                    ["maxLines"] = textArea.maxLines
                };
            }

            if (field.GetCustomAttribute<MultilineAttribute>() is MultilineAttribute multi)
                schema["x-unity-multiline"] = multi.lines;
        }

        private void ApplyValidationAttributes(JObject schema, FieldInfo field)
        {
            if (field.GetCustomAttribute<RangeAttribute>() is RangeAttribute range)
            {
                schema["minimum"] = (double)range.min;
                schema["maximum"] = (double)range.max;
            }

            if (field.GetCustomAttribute<MinAttribute>() is MinAttribute min)
            {
                var existing = schema.TryGetValue("minimum", out var minToken) ? (double?)minToken : null;
                var next = (double)min.min;
                schema["minimum"] = existing.HasValue ? Math.Max(existing.Value, next) : next;
            }
        }

        private void ApplyCustomMetadata(JObject schema,
                                         FieldInfo field,
                                         Dictionary<string, List<AssetBindingInfo>> assetBindingsByMember,
                                         Dictionary<string, List<AssetBindingInfo>> assetBindingsByKey)
        {
            if (field.GetCustomAttribute<IdReferenceAttribute>() is IdReferenceAttribute idRef)
            {
                var meta = new JObject
                {
                    ["targetType"] = idRef.TargetType?.AssemblyQualifiedName ?? string.Empty,
                    ["optional"] = idRef.Optional
                };
                schema["x-id-reference"] = meta;
            }

            if (assetBindingsByMember.TryGetValue(field.Name, out var bindings))
            {
                schema["x-asset-reference"] = new JArray(bindings.Select(b => new JObject
                {
                    ["pathKey"] = b.PathKey,
                    ["optional"] = b.Optional
                }));
                schema["readOnly"] = true;
            }

            if (assetBindingsByKey.TryGetValue(field.Name, out var keyBindings))
            {
                schema["x-asset-path-key"] = new JArray(keyBindings.Select(b => new JObject
                {
                    ["assetField"] = b.MemberName,
                    ["optional"] = b.Optional
                }));
            }
        }

        private void ApplyDefaultValue(JObject schema, object value)
        {
            if (value == null)
                return;

            if (value is UnityEngine.Object)
                return;

            try
            {
                schema["default"] = JToken.FromObject(value, _serializer);
            }
            catch
            {
                // ignore
            }
        }

        private readonly struct AssetBindingInfo
        {
            public AssetBindingInfo(string memberName, string pathKey, bool optional)
            {
                MemberName = memberName;
                PathKey = pathKey;
                Optional = optional;
            }

            public string MemberName { get; }
            public string PathKey { get; }
            public bool Optional { get; }
        }
    }
}
#endif
