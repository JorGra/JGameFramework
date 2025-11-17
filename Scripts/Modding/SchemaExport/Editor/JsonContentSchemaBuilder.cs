#if UNITY_EDITOR
using JG.GameContent;
using JG.GameContent.AssetResolving;
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

        private static readonly Lazy<IReadOnlyList<AssetResolverDescriptor>> ResolverDescriptorsLazy =
            new(() => LoadResolverDescriptors());

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

        private static IReadOnlyList<AssetResolverDescriptor> LoadResolverDescriptors()
        {
            try
            {
                return AssetResolverRegistry.GetResolverDescriptors();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JsonContentSchemaBuilder] Failed to query asset resolvers: {ex.Message}");
                return Array.Empty<AssetResolverDescriptor>();
            }
        }

        private static IReadOnlyList<AssetResolverDescriptor> GetDescriptorsForType(Type memberType)
        {
            var descriptors = ResolverDescriptorsLazy.Value ?? Array.Empty<AssetResolverDescriptor>();
            if (descriptors.Count == 0)
                return descriptors;

            if (memberType == null)
                return descriptors;

            var matches = descriptors.Where(d => d.SupportsType(memberType)).ToList();
            if (matches.Count > 0)
                return matches;

            var generic = descriptors.Where(d => d.SupportedTypes.Count == 0).ToList();
            return generic.Count > 0 ? generic : Array.Empty<AssetResolverDescriptor>();
        }

        private static string DeterminePreviewKind(Type memberType, IReadOnlyList<AssetResolverDescriptor> descriptors)
        {
            if (descriptors != null)
            {
                foreach (var descriptor in descriptors)
                {
                    if (!string.IsNullOrWhiteSpace(descriptor.PreviewKind))
                        return descriptor.PreviewKind;
                }
            }

            if (memberType != null && typeof(Sprite).IsAssignableFrom(memberType))
                return "image";

            return string.Empty;
        }
        private JObject BuildObjectSchema(Type type, object instance)
        {
            var properties = new JObject();
            var required = new JArray();

            var members = EnumerateSerializableMembers(type).ToList();
            var assetBindingsByMember = BuildAssetBindingsByMember(type);
            var assetBindingsByKey = BuildAssetBindingsByKey(assetBindingsByMember);

            foreach (var member in members)
            {
                var value = instance != null ? GetMemberValue(member, instance) : null;
                var schema = BuildSchemaForMember(member, value, assetBindingsByMember, assetBindingsByKey);
                if (schema == null)
                    continue;

                var jsonName = member.Name;
                properties[jsonName] = schema;

                if (IsMemberRequired(member))
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

        private static IEnumerable<MemberInfo> EnumerateSerializableMembers(Type type)
        {
            var stack = new Stack<Type>();
            var current = type;
            while (current != null && current != typeof(ScriptableObject) && current != typeof(UnityEngine.Object))
            {
                stack.Push(current);
                current = current.BaseType;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (stack.Count > 0)
            {
                var levelType = stack.Pop();
                foreach (var member in EnumerateDeclaredSerializableMembers(levelType))
                {
                    if (seen.Add(member.Name))
                        yield return member;
                }
            }
        }

        private static IEnumerable<MemberInfo> EnumerateDeclaredSerializableMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var members = new List<MemberInfo>();

            foreach (var field in type.GetFields(flags))
            {
                if (!IsSerializableField(field))
                    continue;
                members.Add(field);
            }

            foreach (var property in type.GetProperties(flags))
            {
                if (!IsSerializableProperty(property))
                    continue;
                members.Add(property);
            }

            members.Sort(CompareByMetadataToken);
            return members;
        }

        private static int CompareByMetadataToken(MemberInfo left, MemberInfo right)
        {
            try
            {
                return left.MetadataToken.CompareTo(right.MetadataToken);
            }
            catch (InvalidOperationException)
            {
                return string.Compare(left.Name, right.Name, StringComparison.Ordinal);
            }
        }

        private static bool IsSerializableField(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly)
                return false;
            if (field.IsDefined(typeof(NonSerializedAttribute), true))
                return false;
            if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true))
                return false;
            return true;
        }

        private static bool IsSerializableProperty(PropertyInfo property)
        {
            if (property == null)
                return false;

            if (property.GetIndexParameters().Length > 0)
                return false;

            var getter = property.GetGetMethod(true);
            var setter = property.GetSetMethod(true);
            if (getter == null || setter == null)
                return false;

            if (getter.IsStatic || setter.IsStatic)
                return false;

            if (property.IsDefined(typeof(JsonIgnoreAttribute), true))
                return false;

            if (property.IsDefined(typeof(IgnoreDataMemberAttribute), true))
                return false;

            return true;
        }

        private static object GetMemberValue(MemberInfo member, object instance)
        {
            if (instance == null)
                return null;

            try
            {
                return member switch
                {
                    FieldInfo field => field.GetValue(instance),
                    PropertyInfo property => property.GetGetMethod(true)?.Invoke(instance, null),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => null
            };
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
                list.Add(new AssetBindingInfo(member.Name, GetMemberType(member), attr.PathKey, attr.Optional));
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

        private JObject BuildSchemaForMember(MemberInfo member,
                                             object value,
                                             Dictionary<string, List<AssetBindingInfo>> assetBindingsByMember,
                                             Dictionary<string, List<AssetBindingInfo>> assetBindingsByKey)
        {
            var memberType = GetMemberType(member);
            var schema = BuildSchemaForType(memberType, value);
            if (schema == null)
                return null;

            schema["title"] = ObjectNames.NicifyVariableName(member.Name);

            ApplyDocumentation(schema, member);
            ApplyValidationAttributes(schema, member);
            ApplyCustomMetadata(schema, member, assetBindingsByMember, assetBindingsByKey);
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

            if (type.IsInterface || type.IsAbstract)
            {
                var polymorphic = BuildPolymorphicSchema(type, value);
                if (polymorphic != null)
                    return polymorphic;
            }

            if (IsSerializableClassOrStruct(type))
                return BuildReferenceSchema(type, value);

            Debug.LogWarning($"[JsonContentSchemaBuilder] Unsupported field type {type.FullName}. It will be treated as a free-form object.");
            return new JObject { ["type"] = "object" };
        }

        private JObject BuildPolymorphicSchema(Type baseType, object value)
        {
            var subTypes = DiscoverConcreteSubtypes(baseType);
            if (subTypes.Count == 0)
                return null;

            var oneOf = new JArray();
            var mapping = new JObject();

            foreach (var subType in subTypes)
            {
                var refSchema = BuildReferenceSchema(subType, value);
                if (!_definitionKeys.TryGetValue(subType, out var key))
                    continue;

                var discriminatorValue = DetermineDiscriminatorValue(subType);

                if (_definitions.TryGetValue(key, out var defSchema))
                    EnsureDiscriminatorOnDefinition(defSchema, discriminatorValue);

                oneOf.Add(refSchema);
                mapping[discriminatorValue] = $"#/$defs/{key}";
            }

            if (oneOf.Count == 0)
                return null;

            var discriminator = new JObject
            {
                ["propertyName"] = "type",
                ["mapping"] = mapping
            };

            return new JObject
            {
                ["oneOf"] = oneOf,
                ["discriminator"] = discriminator
            };
        }

        private static IReadOnlyList<Type> DiscoverConcreteSubtypes(Type baseType)
        {
            try
            {
                var results = new List<Type>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    if (assembly.IsDynamic)
                        continue;

                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
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

                    foreach (var type in types)
                    {
                        if (type == null || type.IsAbstract || type.IsInterface)
                            continue;

                        if (baseType.IsAssignableFrom(type))
                            results.Add(type);
                    }
                }

                return results
                    .Distinct()
                    .OrderBy(t => t.FullName, StringComparer.Ordinal)
                    .ToList();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private static void EnsureDiscriminatorOnDefinition(JObject schema, string discriminatorValue)
        {
            if (schema == null)
                return;

            if (!(schema["properties"] is JObject props))
            {
                props = new JObject();
                schema["properties"] = props;
            }

            if (!props.TryGetValue("type", out var typeToken) || typeToken.Type != JTokenType.Object)
            {
                props["type"] = new JObject { ["const"] = discriminatorValue, ["title"] = "Type" };
            }
            else if (typeToken is JObject typeObj)
            {
                typeObj["const"] = discriminatorValue;
            }

            if (!(schema["required"] is JArray required))
            {
                required = new JArray();
                schema["required"] = required;
            }

            if (!required.Any(t => t.Type == JTokenType.String && string.Equals(t.Value<string>(), "type", StringComparison.Ordinal)))
                required.Add("type");
        }

        private static string DetermineDiscriminatorValue(Type subType)
        {
            if (subType == null)
                return string.Empty;

            // Prefer ItemEffectAttribute.Id when present, else fall back to the CLR name.
            var prop = subType.GetProperty("TypeId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (prop != null && typeof(string).IsAssignableFrom(prop.PropertyType))
            {
                try
                {
                    var instance = Activator.CreateInstance(subType);
                    var value = prop.GetValue(instance) as string;
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
                catch { }
            }

            return subType.Name;
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
                ["x-unity-collection"] = GetCollectionKind(collectionType)
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

        private static string GetCollectionKind(Type collectionType)
        {
            if (collectionType == null)
                return string.Empty;

            if (collectionType.IsArray)
                return "array";

            // Normalize common and BCL generics/interfaces to "array" for UI simplicity
            if (collectionType.IsGenericType)
            {
                var genDef = collectionType.GetGenericTypeDefinition();
                if (genDef == typeof(List<>) ||
                    genDef == typeof(IList<>) ||
                    genDef == typeof(IReadOnlyList<>) ||
                    genDef == typeof(IEnumerable<>))
                    return "array";
            }

            // Any System.* collection types should render as a plain array
            if (collectionType.Namespace != null && collectionType.Namespace.StartsWith("System", StringComparison.Ordinal))
                return "array";

            return collectionType.FullName;
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

        private static bool IsMemberRequired(MemberInfo member)
            => string.Equals(member.Name, "id", StringComparison.OrdinalIgnoreCase);

        private void ApplyDocumentation(JObject schema, MemberInfo member)
        {
            if (member.GetCustomAttribute<TooltipAttribute>() is TooltipAttribute tooltip && !string.IsNullOrWhiteSpace(tooltip.tooltip))
                schema["description"] = tooltip.tooltip;

            if (member.GetCustomAttribute<HeaderAttribute>() is HeaderAttribute header && !string.IsNullOrWhiteSpace(header.header))
                schema["x-unity-header"] = header.header;

            if (member.GetCustomAttribute<TextAreaAttribute>() is TextAreaAttribute textArea)
            {
                schema["x-unity-text-area"] = new JObject
                {
                    ["minLines"] = textArea.minLines,
                    ["maxLines"] = textArea.maxLines
                };
            }

            if (member.GetCustomAttribute<MultilineAttribute>() is MultilineAttribute multi)
                schema["x-unity-multiline"] = multi.lines;
        }

        private void ApplyValidationAttributes(JObject schema, MemberInfo member)
        {
            if (member.GetCustomAttribute<RangeAttribute>() is RangeAttribute range)
            {
                schema["minimum"] = (double)range.min;
                schema["maximum"] = (double)range.max;
            }

            if (member.GetCustomAttribute<MinAttribute>() is MinAttribute min)
            {
                var existing = schema.TryGetValue("minimum", out var minToken) ? (double?)minToken : null;
                var next = (double)min.min;
                schema["minimum"] = existing.HasValue ? Math.Max(existing.Value, next) : next;
            }
        }

        private void ApplyCustomMetadata(JObject schema,
                                         MemberInfo member,
                                         Dictionary<string, List<AssetBindingInfo>> assetBindingsByMember,
                                         Dictionary<string, List<AssetBindingInfo>> assetBindingsByKey)
        {
            if (member.GetCustomAttribute<IdReferenceAttribute>() is IdReferenceAttribute idRef)
            {
                var meta = new JObject
                {
                    ["targetType"] = idRef.TargetType?.AssemblyQualifiedName ?? string.Empty,
                    ["optional"] = idRef.Optional
                };
                schema["x-id-reference"] = meta;
            }

            if (assetBindingsByMember.TryGetValue(member.Name, out var bindings))
            {
                var referenceArray = new JArray(bindings.Select(CreateAssetBindingMetadata));
                if (referenceArray.Count > 0)
                    schema["x-asset-reference"] = referenceArray;
                schema["readOnly"] = true;
            }

            if (assetBindingsByKey.TryGetValue(member.Name, out var keyBindings))
            {
                var pathArray = new JArray(keyBindings.Select(CreateAssetBindingMetadata));
                if (pathArray.Count > 0)
                {
                    schema["x-asset-path-key"] = pathArray;

                    string previewKind = null;
                    foreach (var token in pathArray)
                    {
                        if (token is JObject obj && obj.TryGetValue("previewKind", out var previewToken) && previewToken.Type == JTokenType.String)
                        {
                            var value = previewToken.Value<string>();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                previewKind = value;
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(previewKind))
                        schema["x-preview-kind"] = previewKind;
                }
            }
        }
        private JObject CreateAssetBindingMetadata(AssetBindingInfo binding)
        {
            var metadata = new JObject
            {
                ["assetField"] = binding.MemberName,
                ["optional"] = binding.Optional,
                ["pathKey"] = binding.PathKey,
                ["assetFieldDisplayName"] = ObjectNames.NicifyVariableName(binding.MemberName)
            };

            if (binding.MemberType != null)
            {
                metadata["assetFieldType"] = binding.MemberType.FullName;
                metadata["assetFieldQualifiedType"] = binding.MemberType.AssemblyQualifiedName;
                metadata["assetFieldTypeName"] = binding.MemberType.Name;
            }

            var descriptors = GetDescriptorsForType(binding.MemberType);
            var supportsResources = descriptors.Count == 0 || descriptors.Any(d => d.SupportsResources);
            var supportsFileSystem = descriptors.Count == 0 || descriptors.Any(d => d.Extensions.Count > 0);

            metadata["supportsResources"] = supportsResources;
            metadata["supportsFileSystem"] = supportsFileSystem;

            if (descriptors.Count > 0)
            {
                var resolverArray = new JArray();
                foreach (var descriptor in descriptors)
                {
                    var resolverObj = new JObject
                    {
                        ["id"] = descriptor.Id,
                        ["label"] = descriptor.DisplayName
                    };

                    if (descriptor.Extensions.Count > 0)
                        resolverObj["extensions"] = new JArray(descriptor.Extensions);

                    if (!string.IsNullOrWhiteSpace(descriptor.PreviewKind))
                        resolverObj["previewKind"] = descriptor.PreviewKind;

                    if (descriptor.SupportedTypes.Count > 0)
                    {
                        resolverObj["supportedTypes"] = new JArray(descriptor.SupportedTypes
                            .Where(t => t != null)
                            .Select(t => t.FullName));
                    }

                    resolverObj["supportsResources"] = descriptor.SupportsResources;

                    resolverArray.Add(resolverObj);
                }

                metadata["resolvers"] = resolverArray;

                var previewKind = DeterminePreviewKind(binding.MemberType, descriptors);
                if (!string.IsNullOrWhiteSpace(previewKind))
                    metadata["previewKind"] = previewKind;
            }

            return metadata;
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
            public AssetBindingInfo(string memberName, Type memberType, string pathKey, bool optional)
            {
                MemberName = memberName;
                MemberType = memberType;
                PathKey = pathKey;
                Optional = optional;
            }

            public string MemberName { get; }
            public Type MemberType { get; }
            public string PathKey { get; }
            public bool Optional { get; }
        }

    }
}
#endif

